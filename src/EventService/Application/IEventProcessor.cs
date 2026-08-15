namespace EventService.Application;

public interface IEventProcessor
{
    Task<ProcessResult> ProcessProfitEventAsync(
        ProfitEventDto eventDto,
        CancellationToken cancellationToken = default);
}

public record ProfitEventRequest(
    string EventExternalId,
    string UserExternalId,
    decimal Profit,
    DateTime OccurredAt);

public record ProcessResult
{
    public bool Success { get; init; }
    public required string Message { get; init; }
}

public record ProfitEventDto
{
    public required string EventExternalId { get; init; }
    public required string UserExternalId { get; init; }
    public decimal Profit { get; init; }
    public DateTime OccurredAt { get; init; }
}
