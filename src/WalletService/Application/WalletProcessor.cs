using Microsoft.EntityFrameworkCore;
using WalletService.Domain;
using WalletService.Infrastructure;

namespace WalletService.Application;

public class WalletProcessor : IWalletProcessor
{
    private readonly WalletDbContext _context;
    private readonly ILogger<WalletProcessor> _logger;

    public WalletProcessor(WalletDbContext context, ILogger<WalletProcessor> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DepositResult> DepositCommissionAsync(
        string userExternalId,
        decimal amount,
        string commissionEventExternalId, CancellationToken cancellationToken)
    {
        var existingTransaction = await _context.Transactions
            .AnyAsync(t =>
                t.UserExternalId == userExternalId &&
                t.CommissionEventExternalId == commissionEventExternalId, cancellationToken);

        if (existingTransaction)
        {
            _logger.LogWarning("Комиссия {CommissionId} уже выплачена пользователю {UserId}",
                commissionEventExternalId, userExternalId);
            return new DepositResult
            {
                Success = false,
                Message = "Commission already paid",
                IsDuplicate = true
            };
        }

        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserExternalId == userExternalId, cancellationToken);

        if (wallet == null)
        {
            wallet = new Wallet(userExternalId);
            await _context.Wallets.AddAsync(wallet, cancellationToken);
        }

        wallet.AddBalance(amount);

        var transaction = new WalletTransaction(
            userExternalId,
            amount,
            commissionEventExternalId);

        await _context.Transactions.AddAsync(transaction, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Начислено {Amount} на кошелек {UserId} по комиссии {CommissionId}",
                amount, userExternalId, commissionEventExternalId);

            return new DepositResult
            {
                Success = true,
                Message = "Commission deposited",
                NewBalance = wallet.Balance
            };
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate key") == true)
        {
            _logger.LogWarning("Попытка двойного начисления комиссии {CommissionId}", commissionEventExternalId);
            return new DepositResult
            {
                Success = false,
                Message = "Duplicate commission",
                IsDuplicate = true
            };
        }
    }

    public async Task<decimal> GetBalance(string userExternalId, CancellationToken cancellationToken)
    {
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserExternalId == userExternalId, cancellationToken);

        return wallet?.Balance ?? 0;
    }

    public async Task<List<WalletTransaction>> GetTransactionHistory(string userExternalId, CancellationToken cancellationToken)
    {
        return await _context.Transactions
            .Where(t => t.UserExternalId == userExternalId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}