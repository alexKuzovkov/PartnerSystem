using System.Text.Json;
using CommissionService.Domain;
using CommissionService.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using PartnerSystem.Contracts;
using PartnerSystem.Contracts.Grpc;

namespace CommissionService.Application;

public class CommissionService : ICommissionService
{
    private readonly CommissionDbContext _context;
    private readonly ICommissionCalculator _calculator;
    private readonly PartnerService.PartnerServiceClient _partnerClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CommissionService> _logger;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
        SlidingExpiration = TimeSpan.FromMinutes(5)
    };

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

    public async Task ProcessCommissionCalculation(CommissionCalculationRequest request)
    {
        _logger.LogInformation(
            "Начало расчета комиссий для события {EventId}, пользователь {UserId}, прибыль {Profit}",
            request.EventExternalId, request.UserExternalId, request.Profit);

        var partnerChain = await GetPartnerChainAsync(request.UserExternalId);

        if (partnerChain.Count == 0)
        {
            _logger.LogWarning("Партнерская цепочка пуста для пользователя {UserId}", request.UserExternalId);
            return;
        }

        _logger.LogInformation(
            "Получена цепочка из {Count} партнеров для события {EventId}",
            partnerChain.Count, request.EventExternalId);

        var commissionResults = _calculator.CalculateChainCommissions(
            request.Profit,
            partnerChain,
            request.SchemaType);

        var savedCommissions = await SaveCommissionsBatchAsync(
            request.EventExternalId,
            commissionResults);

        if (savedCommissions.Count == 0)
        {
            _logger.LogWarning(
                "Все комиссии для события {EventId} уже существуют, пропускаем",
                request.EventExternalId);
            return;
        }

        await PublishPayoutEventsParallelAsync(savedCommissions);

        _logger.LogInformation(
            "Завершено расчет комиссий для события {EventId}. Сохранено {Count} комиссий",
            request.EventExternalId, savedCommissions.Count);
    }


    private async Task<List<Commission>> SaveCommissionsBatchAsync(
        string eventExternalId,
        List<CommissionCalculationResult> commissionResults)
    {
        var existingKeys = await _context.Commissions
            .Where(c => c.EventExternalId == eventExternalId)
            .Select(c => new { c.PartnerExternalId, c.Level })
            .ToListAsync();

        var existingSet = new HashSet<(string Partner, int Level)>(
            existingKeys.Select(k => (k.PartnerExternalId, k.Level)));

        var newCommissions = commissionResults
            .Where(r => !existingSet.Contains((r.PartnerExternalId, r.Level)))
            .Select(r => new Commission(
                eventExternalId,
                r.PartnerExternalId,
                r.Level,
                r.Amount,
                r.SchemaType))
            .ToList();

        if (newCommissions.Count == 0)
        {
            return new List<Commission>();
        }

        await _context.Commissions.AddRangeAsync(newCommissions);

        try
        {
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Сохранено {Count} комиссий для события {EventId} одним батчем",
                newCommissions.Count, eventExternalId);

            return newCommissions;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException?.Message.Contains("duplicate key") == true)
        {
            // Race condition — кто-то успел вставить раньше нас
            _logger.LogWarning(ex,
                "Race condition при сохранении комиссий для события {EventId}. " +
                "Пытаемся сохранить по одной",
                eventExternalId);

            foreach (var entry in _context.ChangeTracker.Entries().ToList())
            {
                entry.State = EntityState.Detached;
            }

            var saved = new List<Commission>();
            foreach (var commission in newCommissions)
            {
                try
                {
                    _context.Commissions.Add(commission);
                    await _context.SaveChangesAsync();
                    saved.Add(commission);
                }
                catch (DbUpdateException)
                {
                    _context.Entry(commission).State = EntityState.Detached;
                }
            }

            return saved;
        }
    }

    private async Task PublishPayoutEventsParallelAsync(List<Commission> commissions)
    {
        var publishTasks = commissions.Select(commission =>
        {
            var payoutEvent = new CommissionCalculated(
                commission.EventExternalId,
                commission.PartnerExternalId,
                commission.Level,
                commission.Amount,
                commission.SchemaType);

            return PublishWithRetryAsync(payoutEvent);
        });

        var results = await Task.WhenAll(publishTasks);

        var successCount = results.Count(r => r);
        var failCount = results.Length - successCount;

        _logger.LogInformation(
            "Опубликовано {Success} событий для выплат, ошибок: {Fail}",
            successCount, failCount);
    }

    private async Task<bool> PublishWithRetryAsync(CommissionCalculated payoutEvent)
    {
        const int maxRetries = 3;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await _publishEndpoint.Publish(payoutEvent);
                return true;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex,
                    "Ошибка публикации события {EventId} (попытка {Attempt}/{Max})",
                    payoutEvent.EventExternalId, attempt, maxRetries);

                await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Критическая ошибка публикации события {EventId} после {Max} попыток",
                    payoutEvent.EventExternalId, maxRetries);
                return false;
            }
        }

        return false;
    }

    public async Task<CommissionDetailsDto> GetCommissionDetails(string eventExternalId)
    {
        var commissions = await _context.Commissions
            .Where(c => c.EventExternalId == eventExternalId)
            .OrderBy(c => c.Level)
            .ToListAsync();

        return new CommissionDetailsDto
        {
            EventExternalId = eventExternalId,
            Commissions = commissions.Select(c => new CommissionItemDto
            {
                PartnerExternalId = c.PartnerExternalId,
                Level = c.Level,
                Amount = c.Amount,
                SchemaType = c.SchemaType,
                IsPaid = c.IsPaid
            }).ToList()
        };
    }

    private async Task<List<string>> GetPartnerChainAsync(string userExternalId)
    {
        var cacheKey = $"partner_chain:{userExternalId}";

        try
        {
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedChain = JsonSerializer.Deserialize<List<string>>(cachedData);
                if (cachedChain != null && cachedChain.Count > 0)
                {
                    _logger.LogDebug(
                        "Цепочка получена из Redis для {UserId} ({Count} уровней)",
                        userExternalId, cachedChain.Count);
                    return cachedChain;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка при чтении из Redis, запрашиваем через gRPC");
        }

        try
        {
            var request = new GetPartnerChainRequest
            {
                UserExternalId = userExternalId,
                MaxLevels = 10
            };

            var response = await _partnerClient.GetPartnerChainAsync(request);
            var chain = response.PartnerExternalIds.ToList();

            if (chain.Count > 0)
            {
                try
                {
                    var jsonData = JsonSerializer.Serialize(chain);
                    await _cache.SetStringAsync(cacheKey, jsonData, CacheOptions);

                    _logger.LogDebug(
                        "Цепочка закэширована в Redis для {UserId} ({Count} уровней)",
                        userExternalId, chain.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка при сохранении в Redis");
                }
            }

            return chain;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении партнерской цепочки для пользователя {UserId}", userExternalId);
            throw;
        }
    }
}