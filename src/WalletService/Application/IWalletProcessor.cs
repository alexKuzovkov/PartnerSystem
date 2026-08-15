using WalletService.Domain;

namespace WalletService.Application;

public interface IWalletProcessor
{
    Task<DepositResult> DepositCommissionAsync(
        string userExternalId,
        decimal amount,
        string commissionIdempotencyKey,
        CancellationToken cancellationToken);

    Task<decimal> GetBalanceAsync(
        string userExternalId,
        CancellationToken cancellationToken);

    Task<List<WalletTransaction>> GetTransactionHistoryAsync(
        string userExternalId,
        CancellationToken cancellationToken);
}

public sealed record DepositResult
{
    public bool Success { get; init; }
    public required string Message { get; init; }
    public bool IsDuplicate { get; init; }
    public decimal NewBalance { get; init; }
}
