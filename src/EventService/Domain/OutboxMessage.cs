namespace EventService.Domain;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public DateTime OccurredOn { get; private set; }
    public string Type { get; private set; } = null!;
    public string Data { get; private set; } = null!;
    public DateTime? ProcessedDate { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string type, string data)
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        Type = type;
        Data = data;
    }

    public void MarkAsProcessed()
    {
        ProcessedDate = DateTime.UtcNow;
    }
}