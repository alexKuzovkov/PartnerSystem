using Microsoft.EntityFrameworkCore;
using WalletService.Application;

namespace WalletService.Infrastructure;

public class PayoutBackgroundService : BackgroundService
{
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
        _payoutInterval = payoutInterval ?? TimeSpan.FromMinutes(5);
        _instanceId = $"{Environment.MachineName}_{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "PayoutBackgroundService запущен. InstanceId: {InstanceId}, Interval: {Interval}",
            _instanceId, _payoutInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await TryAcquireLockAsync(stoppingToken))
                {
                    await ProcessPayoutsAsync(stoppingToken);
                    await ReleaseLockAsync();
                }
                else
                {
                    _logger.LogDebug("Lock занят другим инстансом, пропускаю итерацию");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке выплат");
                await ReleaseLockAsync();
            }

            await Task.Delay(_payoutInterval, stoppingToken);
        }
    }

    private async Task<bool> TryAcquireLockAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

        try
        {
            var result = await context.Database
                .ExecuteSqlRawAsync(
                    "SELECT pg_try_advisory_lock(123456789)",
                    cancellationToken);

            using var cmd = context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = "SELECT pg_try_advisory_lock(123456789)";
            await context.Database.OpenConnectionAsync(cancellationToken);
            var lockResult = await cmd.ExecuteScalarAsync(cancellationToken);

            return lockResult is bool b && b;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при захвате lock");
            return false;
        }
    }

    private async Task ReleaseLockAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

            await context.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_unlock(123456789)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при освобождении lock");
        }
    }

    private async Task ProcessPayoutsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Начало обработки выплат...");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
        var walletProcessor = scope.ServiceProvider.GetRequiredService<IWalletProcessor>();

        var unpaidPayouts = await context.PendingPayouts
            .Where(p => !p.IsPaid && p.LockedAt == null)
            .OrderBy(p => p.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        if (unpaidPayouts.Count == 0)
        {
            _logger.LogDebug("Невыплаченных комиссий не найдено");
            return;
        }

        _logger.LogInformation("Найдено {Count} невыплаченных комиссий", unpaidPayouts.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var payout in unpaidPayouts)
        {
            try
            {
                payout.LockForProcessing(_instanceId);

                var result = await walletProcessor.DepositCommissionAsync(
                    payout.PartnerExternalId,
                    payout.Amount,
                    payout.Id.ToString(), cancellationToken);

                if (result.Success || result.IsDuplicate)
                {
                    payout.MarkAsPaid();
                    successCount++;
                }
                else
                {
                    payout.Unlock();
                    failCount++;
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogDebug("Конфликт при обработке {PayoutId}, пропускаю", payout.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выплате {PayoutId}", payout.Id);
                payout.Unlock();
                failCount++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Обработка завершена. Успешно: {Success}, Ошибок: {Fail}",
            successCount, failCount);
    }
}