using EventService.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using PartnerSystem.Contracts;

namespace EventService.Application;

public class EventProcessor : IEventProcessor
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

    public async Task<ProcessResult> ProcessProfitEvent(ProfitEventDto eventDto)
    {
        var existingEvent = await _context.ProfitEvents
            .FirstOrDefaultAsync(e => e.EventExternalId == eventDto.EventExternalId);

        if (existingEvent != null)
        {
            _logger.LogWarning("Событие {EventId} уже обработано", eventDto.EventExternalId);
            return new ProcessResult { Success = false, Message = "Event already processed" };
        }

        var profitEvent = new Domain.ProfitEvent(
            eventDto.EventExternalId,
            eventDto.UserExternalId,
            eventDto.Profit,
            eventDto.OccurredAt);

        await _context.ProfitEvents.AddAsync(profitEvent);

        if (eventDto.Profit > 0)
        {
            var calculationEvent = new CommissionCalculationRequested(
                eventDto.EventExternalId,
                eventDto.UserExternalId,
                eventDto.Profit,
                eventDto.OccurredAt);

            await _publishEndpoint.Publish(calculationEvent);

            _logger.LogInformation("Опубликовано событие для расчета комиссий по событию {EventId}",
                eventDto.EventExternalId);
        }

        await _context.SaveChangesAsync();

        return new ProcessResult { Success = true, Message = "Event processed" };
    }
}