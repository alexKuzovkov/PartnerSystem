using SchemaType = PartnerSystem.Contracts.SchemaType;

namespace CommissionService.Domain;

public class Commission
{
    public Guid Id { get; private set; }
    public string EventExternalId { get; private set; } = null!;
    public string PartnerExternalId { get; private set; } = null!;
    public int Level { get; private set; }
    public decimal Amount { get; private set; }
    public SchemaType SchemaType { get; private set; }
    public bool IsPaid { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Commission() { }

    public Commission(
        string eventExternalId,
        string partnerExternalId,
        int level,
        decimal amount,
        SchemaType schemaType)
    {
        Id = Guid.NewGuid();
        EventExternalId = eventExternalId;
        PartnerExternalId = partnerExternalId;
        Level = level;
        Amount = amount;
        SchemaType = schemaType;
        IsPaid = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsPaid()
    {
        IsPaid = true;
    }
}