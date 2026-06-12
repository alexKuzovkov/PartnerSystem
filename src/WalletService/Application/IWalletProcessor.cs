using WalletService.Domain;

namespace WalletService.Application;

public interface IWalletProcessor
{
    Task<DepositResult> DepositCommissionAsync(
        string userExternalId,
        decimal amount,
        string commissionEventExternalId, 
        CancellationToken cancellationToken);

    Task<decimal> GetBalance(string userExternalId, CancellationToken cancellationToken);
    Task<List<WalletTransaction>> GetTransactionHistory(string userExternalId, CancellationToken cancellationToken);
}

public record DepositResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = null!;
    public bool IsDuplicate { get; init; }
    public decimal NewBalance { get; init; }
}