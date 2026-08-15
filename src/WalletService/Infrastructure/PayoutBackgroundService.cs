using Microsoft.EntityFrameworkCore;
using WalletService.Application;

namespace WalletService.Infrastructure;

/// <summary>
/// Claims payout rows in short PostgreSQL transactions using FOR UPDATE SKIP LOCKED.
/// Multiple WalletService instances can therefore process different batches concurrently
/// without a process-wide or database-wide advisory lock.
/// </summary>
public sealed class PayoutBackgroundService : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan ClaimTimeout = TimeSpan.FromMinutes(2);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PayoutBackgroundService> _logger;
    private readonly TimeSpan _payoutInterval;
    private readonly string _instanceId;

    public PayoutBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<PayoutBackgroundService> logger,
        TimeSpan? payoutInterval = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _payoutInterval = payoutInterval ?? TimeSpan.FromSeconds(30);
        _instanceId = $"{Environment.MachineName}_{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "PayoutBackgroundService started. InstanceId: {InstanceId}, interval: {Interval}",
            _instanceId,
            _payoutInterval);

        using var timer = new PeriodicTimer(_payoutInterval);

        do
        {
            try
            {
                var claimedPayoutIds = await ClaimBatchAsync(stoppingToken);
                foreach (var payoutId in claimedPayoutIds)
                    await ProcessClaimedPayoutAsync(payoutId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing payout batch");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));

        _logger.LogInformation("PayoutBackgroundService stopped");
    }

    private async Task<List<Guid>> ClaimBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var staleBefore = DateTime.UtcNow.Subtract(ClaimTimeout);

        var payouts = await context.PendingPayouts
            .FromSqlInterpolated($"""
                SELECT *
                FROM "PendingPayouts"
                WHERE NOT "IsPaid"
                  AND ("LockedAt" IS NULL OR "LockedAt" < {staleBefore})
                ORDER BY "CreatedAt"
                FOR UPDATE SKIP LOCKED
                LIMIT {BatchSize}
                """)
            .ToListAsync(cancellationToken);

        foreach (var payout in payouts)
            payout.LockForProcessing(_instanceId);

        if (payouts.Count > 0)
            await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        if (payouts.Count > 0)
        {
            _logger.LogDebug(
                "Instance {InstanceId} claimed {Count} payouts",
                _instanceId,
                payouts.Count);
        }

        return payouts.Select(p => p.Id).ToList();
    }

    private async Task ProcessClaimedPayoutAsync(
        Guid payoutId,
        CancellationToken cancellationToken)
    {
        var snapshot = await LoadClaimedPayoutAsync(payoutId, cancellationToken);
        if (snapshot is null)
            return;

        try
        {
            DepositResult result;
            await using (var walletScope = _serviceProvider.CreateAsyncScope())
            {
                var walletProcessor = walletScope.ServiceProvider.GetRequiredService<IWalletProcessor>();

                // Payout.Id is stable across retries. If the process crashes after the wallet
                // transaction commits but before the payout row is marked paid, the next attempt
                // uses the same key and WalletProcessor safely recognizes the duplicate.
                result = await walletProcessor.DepositCommissionAsync(
                    snapshot.PartnerExternalId,
                    snapshot.Amount,
                    snapshot.Id.ToString("N"),
                    cancellationToken);
            }

            if (result.Success || result.IsDuplicate)
            {
                await MarkPayoutCompletedAsync(snapshot.Id, cancellationToken);
                return;
            }

            await ReleaseClaimAsync(snapshot.Id, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The claim will become available after ClaimTimeout if shutdown interrupts processing.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process payout {PayoutId}", snapshot.Id);
            await ReleaseClaimAsync(snapshot.Id, CancellationToken.None);
        }
    }

    private async Task<ClaimedPayout?> LoadClaimedPayoutAsync(
        Guid payoutId,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        return await context.PendingPayouts
            .AsNoTracking()
            .Where(p =>
                p.Id == payoutId &&
                !p.IsPaid &&
                p.LockedByInstance == _instanceId)
            .Select(p => new ClaimedPayout(
                p.Id,
                p.PartnerExternalId,
                p.Amount))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task MarkPayoutCompletedAsync(
        Guid payoutId,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        var payout = await context.PendingPayouts.SingleOrDefaultAsync(
            p => p.Id == payoutId &&
                 !p.IsPaid &&
                 p.LockedByInstance == _instanceId,
            cancellationToken);

        if (payout is null)
            return;

        payout.MarkAsPaid();
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Payout {PayoutId} completed for partner {PartnerId}",
            payout.Id,
            payout.PartnerExternalId);
    }

    private async Task ReleaseClaimAsync(
        Guid payoutId,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        var payout = await context.PendingPayouts.SingleOrDefaultAsync(
            p => p.Id == payoutId &&
                 !p.IsPaid &&
                 p.LockedByInstance == _instanceId,
            cancellationToken);

        if (payout is null)
            return;

        payout.Unlock();
        await context.SaveChangesAsync(cancellationToken);
    }

    private sealed record ClaimedPayout(
        Guid Id,
        string PartnerExternalId,
        decimal Amount);
}
