namespace WalletService.Domain;

public class Wallet
{
    public Guid Id { get; private set; }
    public string UserExternalId { get; private set; } = null!;
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Wallet() { }

    public Wallet(string userExternalId)
    {
        Id = Guid.NewGuid();
        UserExternalId = userExternalId;
        Balance = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddBalance(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));
        if (amount > Balance)
            throw new InvalidOperationException("Insufficient funds");

        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }
}