using CommissionService.Application;
using MassTransit;
using PartnerSystem.Contracts;

namespace CommissionService.Consumers;

public sealed class CommissionCalculationConsumer : IConsumer<CommissionCalculationRequested>
{
    private readonly ICommissionService _commissionService;
    private readonly ISchemaSettingsService _schemaSettingsService;
    private readonly ILogger<CommissionCalculationConsumer> _logger;

    public CommissionCalculationConsumer(
        ICommissionService commissionService,
        ISchemaSettingsService schemaSettingsService,
        ILogger<CommissionCalculationConsumer> logger)
    {
        _commissionService = commissionService;
        _schemaSettingsService = schemaSettingsService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CommissionCalculationRequested> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received commission calculation request {EventId} for user {UserId}, profit {Profit}",
            message.EventExternalId,
            message.UserExternalId,
            message.Profit);

        // The active commission scheme is evaluated at processing time and then persisted on
        // each Commission row, so changing the setting never recalculates historical records.
        var currentSchema = await _schemaSettingsService.GetCurrentSchemaAsync(context.CancellationToken);

        await _commissionService.ProcessCommissionCalculationAsync(
            new CommissionCalculationRequest
            {
                EventExternalId = message.EventExternalId,
                UserExternalId = message.UserExternalId,
                Profit = message.Profit,
                OccurredAt = message.OccurredAt,
                SchemaType = currentSchema
            },
            context.CancellationToken);

        _logger.LogInformation(
            "Commission request {EventId} processed using {Schema} scheme",
            message.EventExternalId,
            currentSchema);
    }
}
