using MassTransit;
using PartnerSystem.Contracts;
using WalletService.Domain;
using WalletService.Infrastructure;

namespace WalletService.Application;

public class PayoutConsumer : IConsumer<CommissionCalculated>
{
    private readonly WalletDbContext _context;
    private readonly ILogger<PayoutConsumer> _logger;

    public PayoutConsumer(
        WalletDbContext context,
        ILogger<PayoutConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CommissionCalculated> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Получено событие CommissionCalculated: событие {EventId}, партнер {PartnerId}, сумма {Amount}, уровень {Level}",
            message.EventExternalId,
            message.PartnerExternalId,
            message.Amount,
            message.Level);

        var pendingPayout = new PendingPayout(
            message.EventExternalId,
            message.PartnerExternalId,
            message.Amount,
            message.Level);

        await _context.PendingPayouts.AddAsync(pendingPayout);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Создана запись о выплате {PayoutId} на сумму {Amount} для партнера {PartnerId}",
            pendingPayout.Id,
            pendingPayout.Amount,
            pendingPayout.PartnerExternalId);
    }
}