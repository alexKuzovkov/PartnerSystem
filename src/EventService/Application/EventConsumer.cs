using MassTransit;
using PartnerSystem.Contracts;

namespace EventService.Application;

public sealed class ProfitEventConsumer : IConsumer<ProfitEvent>
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
            "Received ProfitEvent {EventId} for user {UserId}, profit {Profit}",
            message.EventExternalId,
            message.UserExternalId,
            message.Profit);

        var result = await _processor.ProcessProfitEventAsync(
            new ProfitEventDto
            {
                EventExternalId = message.EventExternalId,
                UserExternalId = message.UserExternalId,
                Profit = message.Profit,
                OccurredAt = message.OccurredAt
            },
            context.CancellationToken);

        _logger.LogInformation(
            "ProfitEvent {EventId} processing result: {Result}",
            message.EventExternalId,
            result.Message);
    }
}
