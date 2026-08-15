using Microsoft.EntityFrameworkCore;
using WalletService.Domain;
using WalletService.Infrastructure;

namespace WalletService.Application;

public sealed class WalletProcessor : IWalletProcessor
{
    private readonly WalletDbContext _context;
    private readonly ILogger<WalletProcessor> _logger;

    public WalletProcessor(
        WalletDbContext context,
        ILogger<WalletProcessor> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DepositResult> DepositCommissionAsync(
        string userExternalId,
        decimal amount,
        string commissionIdempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userExternalId))
            throw new ArgumentException("User external id is required.", nameof(userExternalId));
        if (string.IsNullOrWhiteSpace(commissionIdempotencyKey))
            throw new ArgumentException("Commission idempotency key is required.", nameof(commissionIdempotencyKey));
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Commission amount must be positive.");

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var transactionId = Guid.NewGuid();

        // The transaction row is the idempotency gate. PostgreSQL evaluates the unique index
        // atomically, so concurrent workers cannot credit the same commission twice.
        var insertedTransactions = await _context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Transactions"
                ("Id", "UserExternalId", "Amount", "CommissionEventExternalId", "CreatedAt")
            VALUES
                ({transactionId}, {userExternalId}, {amount}, {commissionIdempotencyKey}, {now})
            ON CONFLICT ("UserExternalId", "CommissionEventExternalId") DO NOTHING
            """, cancellationToken);

        if (insertedTransactions == 0)
        {
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogInformation(
                "Commission {CommissionId} was already deposited for user {UserId}",
                commissionIdempotencyKey,
                userExternalId);

            return new DepositResult
            {
                Success = false,
                Message = "Commission already deposited",
                IsDuplicate = true
            };
        }

        // Upsert + increment prevents lost updates when different service instances credit the
        // same wallet concurrently.
        var walletId = Guid.NewGuid();
        await _context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Wallets"
                ("Id", "UserExternalId", "Balance", "CreatedAt", "UpdatedAt")
            VALUES
                ({walletId}, {userExternalId}, {amount}, {now}, {now})
            ON CONFLICT ("UserExternalId") DO UPDATE
            SET "Balance" = "Wallets"."Balance" + EXCLUDED."Balance",
                "UpdatedAt" = EXCLUDED."UpdatedAt"
            """, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var newBalance = await _context.Wallets
            .AsNoTracking()
            .Where(w => w.UserExternalId == userExternalId)
            .Select(w => w.Balance)
            .SingleAsync(cancellationToken);

        _logger.LogInformation(
            "Deposited {Amount} into wallet {UserId} for commission {CommissionId}",
            amount,
            userExternalId,
            commissionIdempotencyKey);

        return new DepositResult
        {
            Success = true,
            Message = "Commission deposited",
            NewBalance = newBalance
        };
    }

    public async Task<decimal> GetBalanceAsync(
        string userExternalId,
        CancellationToken cancellationToken)
    {
        var wallet = await _context.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                w => w.UserExternalId == userExternalId,
                cancellationToken);

        return wallet?.Balance ?? 0m;
    }

    public Task<List<WalletTransaction>> GetTransactionHistoryAsync(
        string userExternalId,
        CancellationToken cancellationToken) =>
        _context.Transactions
            .AsNoTracking()
            .Where(t => t.UserExternalId == userExternalId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
}
