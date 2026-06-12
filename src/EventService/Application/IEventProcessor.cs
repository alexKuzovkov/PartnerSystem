namespace EventService.Application;

public interface IEventProcessor
{
    Task<ProcessResult> ProcessProfitEvent(ProfitEventDto eventDto);
}

public record ProfitEventRequest(
    string EventExternalId,
    string UserExternalId,
    decimal Profit,
    DateTime OccurredAt);

public record ProcessResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = null!;
}

public record ProfitEventDto
{
    public string EventExternalId { get; init; } = null!;
    public string UserExternalId { get; init; } = null!;
    public decimal Profit { get; init; }
    public DateTime OccurredAt { get; init; }
}