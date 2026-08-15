using EventService.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PartnerSystem.Contracts;

namespace EventService.Application;

public sealed class EventProcessor : IEventProcessor
{
    private readonly EventDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<EventProcessor> _logger;

    public EventProcessor(
        EventDbContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<EventProcessor> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<ProcessResult> ProcessProfitEventAsync(
        ProfitEventDto eventDto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventDto);

        var alreadyExists = await _context.ProfitEvents
            .AsNoTracking()
            .AnyAsync(
                e => e.EventExternalId == eventDto.EventExternalId,
                cancellationToken);

        if (alreadyExists)
        {
            _logger.LogInformation(
                "Profit event {EventId} was already processed",
                eventDto.EventExternalId);

            return new ProcessResult
            {
                Success = false,
                Message = "Event already processed"
            };
        }

        var profitEvent = new Domain.ProfitEvent(
            eventDto.EventExternalId,
            eventDto.UserExternalId,
            eventDto.Profit,
            eventDto.OccurredAt);

        await _context.ProfitEvents.AddAsync(profitEvent, cancellationToken);

        if (eventDto.Profit > 0)
        {
            // With MassTransit EF Bus Outbox enabled, the outbound message is persisted in the
            // same database transaction as ProfitEvent when SaveChangesAsync succeeds.
            await _publishEndpoint.Publish(
                new CommissionCalculationRequested(
                    eventDto.EventExternalId,
                    eventDto.UserExternalId,
                    eventDto.Profit,
                    eventDto.OccurredAt),
                cancellationToken);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogInformation(
                ex,
                "Concurrent request already persisted profit event {EventId}",
                eventDto.EventExternalId);

            return new ProcessResult
            {
                Success = false,
                Message = "Event already processed"
            };
        }

        _logger.LogInformation(
            "Profit event {EventId} persisted successfully",
            eventDto.EventExternalId);

        return new ProcessResult
        {
            Success = true,
            Message = "Event processed"
        };
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}
