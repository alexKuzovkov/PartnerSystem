namespace UserService.Domain;

public class User
{
    public Guid Id { get; private set; }
    public string ExternalId { get; private set; } = null!;
    public Guid? ParentId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public User? Parent { get; private set; }

    private User() { }

    public User(string externalId, Guid? parentId = null)
    {
        Id = Guid.NewGuid();
        ExternalId = externalId;
        ParentId = parentId;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetParent(Guid? parentId)
    {
        ParentId = parentId;
    }
}
