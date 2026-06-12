using CommissionService.Application;
using MassTransit;
using Microsoft.Extensions.Logging;
using PartnerSystem.Contracts;

namespace CommissionService.Consumers;

public class CommissionCalculationConsumer : IConsumer<CommissionCalculationRequested>
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
            "📨 Получен запрос на расчет комиссий: {EventId}, пользователь {User}, прибыль {Profit}",
            message.EventExternalId, message.UserExternalId, message.Profit);

        try
        {
            // ✅ Получаем ТЕКУЩУЮ схему из БД (а не из сообщения!)
            var currentSchema = await _schemaSettingsService.GetCurrentSchemaAsync();

            _logger.LogInformation(
                "Текущая схема начисления: {Schema}",
                currentSchema);

            await _commissionService.ProcessCommissionCalculation(
                new CommissionCalculationRequest
                {
                    EventExternalId = message.EventExternalId,
                    UserExternalId = message.UserExternalId,
                    Profit = message.Profit,
                    OccurredAt = message.OccurredAt,
                    SchemaType = currentSchema  // ← Берём из настроек!
                });

            _logger.LogInformation(
                "✅ Комиссии для события {EventId} рассчитаны по схеме {Schema}",
                message.EventExternalId, currentSchema);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Ошибка при расчете комиссий для события {EventId}",
                message.EventExternalId);
            throw;  // MassTransit повторит доставку
        }
    }
}