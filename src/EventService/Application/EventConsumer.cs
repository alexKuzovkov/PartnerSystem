using MassTransit;
using PartnerSystem.Contracts;

namespace EventService.Application;

public class ProfitEventConsumer : IConsumer<ProfitEvent>
{
    private readonly IEventProcessor _processor;
    private readonly ILogger<ProfitEventConsumer> _logger;

    public ProfitEventConsumer(
        IEventProcessor processor,
        ILogger<ProfitEventConsumer> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProfitEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "📨 Получено событие ProfitEvent: {EventId}, пользователь {UserId}, прибыль {Profit}",
            message.EventExternalId,
            message.UserExternalId,
            message.Profit);

        var eventDto = new ProfitEventDto
        {
            EventExternalId = message.EventExternalId,
            UserExternalId = message.UserExternalId,
            Profit = message.Profit,
            OccurredAt = message.OccurredAt
        };

        var result = await _processor.ProcessProfitEvent(eventDto);

        if (result.Success)
        {
            _logger.LogInformation("Событие {EventId} успешно обработано", message.EventExternalId);
        }
        else
        {
            _logger.LogWarning("Событие {EventId} не обработано: {Message}",
                message.EventExternalId, result.Message);
        }
    }
}