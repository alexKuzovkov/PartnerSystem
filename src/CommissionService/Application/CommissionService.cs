using System.Text.Json;
using CommissionService.Domain;
using CommissionService.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using PartnerSystem.Contracts;
using PartnerSystem.Contracts.Grpc;

namespace CommissionService.Application;

public sealed class CommissionService : ICommissionService
{
    private const int PartnerChainMaxLevels = 10;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
        SlidingExpiration = TimeSpan.FromMinutes(5)
    };

    private readonly CommissionDbContext _context;
    private readonly ICommissionCalculator _calculator;
    private readonly PartnerService.PartnerServiceClient _partnerClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CommissionService> _logger;

    public CommissionService(
        CommissionDbContext context,
        ICommissionCalculator calculator,
        PartnerService.PartnerServiceClient partnerClient,
        IPublishEndpoint publishEndpoint,
        IDistributedCache cache,
        ILogger<CommissionService> logger)
    {
        _context = context;
        _calculator = calculator;
        _partnerClient = partnerClient;
        _publishEndpoint = publishEndpoint;
        _cache = cache;
        _logger = logger;
    }

    public async Task ProcessCommissionCalculationAsync(
        CommissionCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation(
            "Starting commission calculation for event {EventId}, user {UserId}, profit {Profit}",
            request.EventExternalId,
            request.UserExternalId,
            request.Profit);

        var partnerChain = await GetPartnerChainAsync(request.UserExternalId, cancellationToken);
        if (partnerChain.Count == 0)
        {
            _logger.LogWarning(
                "Partner chain is empty for user {UserId}; event {EventId} produces no commissions",
                request.UserExternalId,
                request.EventExternalId);
            return;
        }

        var calculationResults = _calculator.CalculateChainCommissions(
            request.Profit,
            partnerChain,
            request.SchemaType);

        if (calculationResults.Count == 0)
        {
            _logger.LogInformation(
                "Event {EventId} produced no positive commissions",
                request.EventExternalId);
            return;
        }

        var existingKeys = await _context.Commissions
            .AsNoTracking()
            .Where(c => c.EventExternalId == request.EventExternalId)
            .Select(c => new { c.PartnerExternalId, c.Level })
            .ToListAsync(cancellationToken);

        var existingSet = existingKeys
            .Select(k => (k.PartnerExternalId, k.Level))
            .ToHashSet();

        var commissions = calculationResults
            .Where(result => !existingSet.Contains((result.PartnerExternalId, result.Level)))
            .Select(result => new Commission(
                request.EventExternalId,
                result.PartnerExternalId,
                result.Level,
                result.Amount,
                result.SchemaType))
            .ToList();

        if (commissions.Count == 0)
        {
            _logger.LogInformation(
                "All commissions for event {EventId} already exist; skipping duplicate processing",
                request.EventExternalId);
            return;
        }

        await _context.Commissions.AddRangeAsync(commissions, cancellationToken);

        // Publishing happens before SaveChanges. With MassTransit EF Bus Outbox enabled for this
        // DbContext, commission rows and outbound messages are committed atomically.
        foreach (var commission in commissions)
        {
            await _publishEndpoint.Publish(
                new CommissionCalculated(
                    commission.EventExternalId,
                    commission.PartnerExternalId,
                    commission.Level,
                    commission.Amount,
                    commission.SchemaType),
                cancellationToken);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // The database unique constraint is the final idempotency boundary. A concurrent
            // redelivery may have committed the same commission set first.
            _logger.LogInformation(
                ex,
                "A concurrent delivery already persisted commissions for event {EventId}",
                request.EventExternalId);
            return;
        }

        _logger.LogInformation(
            "Completed commission calculation for event {EventId}; persisted {Count} commissions",
            request.EventExternalId,
            commissions.Count);
    }

    public async Task<CommissionDetailsDto> GetCommissionDetailsAsync(
        string eventExternalId,
        CancellationToken cancellationToken = default)
    {
        var commissions = await _context.Commissions
            .AsNoTracking()
            .Where(c => c.EventExternalId == eventExternalId)
            .OrderBy(c => c.Level)
            .ToListAsync(cancellationToken);

        return new CommissionDetailsDto
        {
            EventExternalId = eventExternalId,
            Commissions = commissions.Select(c => new CommissionItemDto
            {
                PartnerExternalId = c.PartnerExternalId,
                Level = c.Level,
                Amount = c.Amount,
                SchemaType = c.SchemaType
            }).ToList()
        };
    }

    private async Task<List<string>> GetPartnerChainAsync(
        string userExternalId,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"partner_chain:{userExternalId}";

        try
        {
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrWhiteSpace(cachedData))
            {
                var cachedChain = JsonSerializer.Deserialize<List<string>>(cachedData);
                if (cachedChain is { Count: > 0 })
                {
                    _logger.LogDebug(
                        "Partner chain loaded from Redis for {UserId} ({Count} levels)",
                        userExternalId,
                        cachedChain.Count);
                    return cachedChain;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis read failed; falling back to UserService gRPC");
        }

        try
        {
            var response = await _partnerClient.GetPartnerChainAsync(
                new GetPartnerChainRequest
                {
                    UserExternalId = userExternalId,
                    MaxLevels = PartnerChainMaxLevels
                },
                cancellationToken: cancellationToken);

            var chain = response.PartnerExternalIds.ToList();
            if (chain.Count == 0)
                return chain;

            try
            {
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(chain),
                    CacheOptions,
                    cancellationToken);

                _logger.LogDebug(
                    "Partner chain cached in Redis for {UserId} ({Count} levels)",
                    userExternalId,
                    chain.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis write failed; continuing without cache update");
            }

            return chain;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to retrieve partner chain for user {UserId}",
                userExternalId);
            throw;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}
