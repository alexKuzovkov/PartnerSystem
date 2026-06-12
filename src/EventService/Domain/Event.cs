namespace EventService.Domain;

public class ProfitEvent
{
    public Guid Id { get; private set; }
    public string EventExternalId { get; private set; } = null!;
    public string UserExternalId { get; private set; } = null!;
    public decimal Profit { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ProfitEvent() { }

    public ProfitEvent(string eventExternalId, string userExternalId, decimal profit, DateTime occurredAt)
    {
        Id = Guid.NewGuid();
        EventExternalId = eventExternalId;
        UserExternalId = userExternalId;
        Profit = profit;
        OccurredAt = occurredAt;
        CreatedAt = DateTime.UtcNow;
    }
}