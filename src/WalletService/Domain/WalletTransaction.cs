namespace WalletService.Domain;

public class WalletTransaction
{
    public Guid Id { get; private set; }
    public string UserExternalId { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string CommissionEventExternalId { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private WalletTransaction() { }

    public WalletTransaction(string userExternalId, decimal amount, string commissionEventExternalId)
    {
        Id = Guid.NewGuid();
        UserExternalId = userExternalId;
        Amount = amount;
        CommissionEventExternalId = commissionEventExternalId;
        CreatedAt = DateTime.UtcNow;
    }
}