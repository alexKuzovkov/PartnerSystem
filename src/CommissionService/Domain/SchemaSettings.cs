using PartnerSystem.Contracts;

namespace CommissionService.Domain;

public sealed class SchemaSettings
{
    private SchemaSettings()
    {
    }

    public SchemaSettings(SchemaType initialSchema = SchemaType.Linear)
    {
        Id = Guid.NewGuid();
        CurrentSchema = initialSchema;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "system";
    }

    public Guid Id { get; private set; }
    public SchemaType CurrentSchema { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string UpdatedBy { get; private set; } = null!;

    public void ChangeSchema(SchemaType newSchema, string changedBy)
    {
        if (string.IsNullOrWhiteSpace(changedBy))
            throw new ArgumentException("Changed-by value is required.", nameof(changedBy));

        CurrentSchema = newSchema;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = changedBy;
    }
}
