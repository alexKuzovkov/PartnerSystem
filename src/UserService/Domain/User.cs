namespace UserService.Domain;

public sealed class User
{
    private User()
    {
    }

    public User(string externalId, Guid? parentId = null)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("External id is required.", nameof(externalId));

        Id = Guid.NewGuid();
        ExternalId = externalId.Trim();
        ParentId = parentId;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string ExternalId { get; private set; } = null!;
    public Guid? ParentId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public User? Parent { get; private set; }

    public void SetParent(Guid? parentId)
    {
        if (parentId == Id)
            throw new InvalidOperationException("A user cannot be its own parent.");

        ParentId = parentId;
    }
}
