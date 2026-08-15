using MassTransit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PartnerSystem.Contracts;
using WalletService.Domain;
using WalletService.Infrastructure;

namespace WalletService.Application;

public sealed class PayoutConsumer : IConsumer<CommissionCalculated>
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
            "Received CommissionCalculated event {EventId}, partner {PartnerId}, amount {Amount}, level {Level}",
            message.EventExternalId,
            message.PartnerExternalId,
            message.Amount,
            message.Level);

        var exists = await _context.PendingPayouts
            .AsNoTracking()
            .AnyAsync(
                p => p.CommissionEventExternalId == message.EventExternalId &&
                     p.PartnerExternalId == message.PartnerExternalId &&
                     p.Level == message.Level,
                context.CancellationToken);

        if (exists)
        {
            _logger.LogInformation(
                "Pending payout already exists for event {EventId}, partner {PartnerId}, level {Level}",
                message.EventExternalId,
                message.PartnerExternalId,
                message.Level);
            return;
        }

        var pendingPayout = new PendingPayout(
            message.EventExternalId,
            message.PartnerExternalId,
            message.Amount,
            message.Level);

        await _context.PendingPayouts.AddAsync(pendingPayout, context.CancellationToken);

        try
        {
            await _context.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            _logger.LogInformation(
                ex,
                "Concurrent delivery already created payout for event {EventId}, partner {PartnerId}, level {Level}",
                message.EventExternalId,
                message.PartnerExternalId,
                message.Level);
            return;
        }

        _logger.LogInformation(
            "Created pending payout {PayoutId} for {Amount} to partner {PartnerId}",
            pendingPayout.Id,
            pendingPayout.Amount,
            pendingPayout.PartnerExternalId);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
}
