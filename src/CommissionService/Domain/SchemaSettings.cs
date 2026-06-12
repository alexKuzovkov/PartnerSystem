using PartnerSystem.Contracts;

public class SchemaSettings
{
    public Guid Id { get; private set; }
    public SchemaType CurrentSchema { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string UpdatedBy { get; private set; } = null!;

    private SchemaSettings() { }

    public SchemaSettings(SchemaType initialSchema = SchemaType.Linear)
    {
        Id = Guid.NewGuid();
        CurrentSchema = initialSchema;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = "system";
    }

    public void ChangeSchema(SchemaType newSchema, string changedBy)
    {
        CurrentSchema = newSchema;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = changedBy;
    }
}