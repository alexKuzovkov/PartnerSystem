namespace WalletService.Domain;

public sealed class Wallet
{
    private Wallet()
    {
    }

    public Wallet(string userExternalId)
    {
        if (string.IsNullOrWhiteSpace(userExternalId))
            throw new ArgumentException("User external id is required.", nameof(userExternalId));

        Id = Guid.NewGuid();
        UserExternalId = userExternalId.Trim();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public string UserExternalId { get; private set; } = null!;
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public void AddBalance(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        if (amount > Balance)
            throw new InvalidOperationException("Insufficient funds.");

        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
    }
}
