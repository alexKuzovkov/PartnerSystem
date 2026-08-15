namespace WalletService.Domain;

public sealed class PendingPayout
{
    private PendingPayout()
    {
    }

    public PendingPayout(
        string commissionEventExternalId,
        string partnerExternalId,
        decimal amount,
        int level)
    {
        if (string.IsNullOrWhiteSpace(commissionEventExternalId))
            throw new ArgumentException("Commission event id is required.", nameof(commissionEventExternalId));
        if (string.IsNullOrWhiteSpace(partnerExternalId))
            throw new ArgumentException("Partner external id is required.", nameof(partnerExternalId));
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Payout amount must be positive.");
        if (level is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(level), "Payout level must be between 1 and 10.");

        Id = Guid.NewGuid();
        CommissionEventExternalId = commissionEventExternalId;
        PartnerExternalId = partnerExternalId;
        Amount = amount;
        Level = level;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string CommissionEventExternalId { get; private set; } = null!;
    public string PartnerExternalId { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public int Level { get; private set; }
    public bool IsPaid { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public string? LockedByInstance { get; private set; }
    public DateTime? LockedAt { get; private set; }

    public void MarkAsPaid()
    {
        if (IsPaid)
            return;

        IsPaid = true;
        PaidAt = DateTime.UtcNow;
        Unlock();
    }

    public void LockForProcessing(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("Instance id is required.", nameof(instanceId));
        if (IsPaid)
            throw new InvalidOperationException("A paid payout cannot be claimed for processing.");

        LockedByInstance = instanceId;
        LockedAt = DateTime.UtcNow;
    }

    public void Unlock()
    {
        LockedByInstance = null;
        LockedAt = null;
    }
}
