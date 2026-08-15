using PartnerSystem.Contracts;

namespace CommissionService.Domain;

public sealed class Commission
{
    private Commission()
    {
    }

    public Commission(
        string eventExternalId,
        string partnerExternalId,
        int level,
        decimal amount,
        SchemaType schemaType)
    {
        if (string.IsNullOrWhiteSpace(eventExternalId))
            throw new ArgumentException("Event external id is required.", nameof(eventExternalId));
        if (string.IsNullOrWhiteSpace(partnerExternalId))
            throw new ArgumentException("Partner external id is required.", nameof(partnerExternalId));
        if (level is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(level), "Commission level must be between 1 and 10.");
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Commission amount must be positive.");

        Id = Guid.NewGuid();
        EventExternalId = eventExternalId;
        PartnerExternalId = partnerExternalId;
        Level = level;
        Amount = amount;
        SchemaType = schemaType;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string EventExternalId { get; private set; } = null!;
    public string PartnerExternalId { get; private set; } = null!;
    public int Level { get; private set; }
    public decimal Amount { get; private set; }
    public SchemaType SchemaType { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
