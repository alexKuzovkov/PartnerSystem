namespace WalletService.Domain;

public class PendingPayout
{
    public Guid Id { get; private set; }
    public string CommissionEventExternalId { get; private set; } = null!;
    public string PartnerExternalId { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public int Level { get; private set; }
    public bool IsPaid { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? PaidAt { get; private set; }

    public byte[] RowVersion { get; private set; } = null!;

    public string? LockedByInstance { get; private set; }
    public DateTime? LockedAt { get; private set; }

    private PendingPayout() { }

    public PendingPayout(
        string commissionEventExternalId,
        string partnerExternalId,
        decimal amount,
        int level)
    {
        Id = Guid.NewGuid();
        CommissionEventExternalId = commissionEventExternalId;
        PartnerExternalId = partnerExternalId;
        Amount = amount;
        Level = level;
        IsPaid = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsPaid()
    {
        IsPaid = true;
        PaidAt = DateTime.UtcNow;
        LockedByInstance = null;
        LockedAt = null;
    }

    public void LockForProcessing(string instanceId)
    {
        LockedByInstance = instanceId;
        LockedAt = DateTime.UtcNow;
    }

    public void Unlock()
    {
        LockedByInstance = null;
        LockedAt = null;
    }
}