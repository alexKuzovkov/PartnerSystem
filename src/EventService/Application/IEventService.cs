namespace EventService.Application;

public interface IEventService
{
    Task<List<EventSummaryDto>> GetUserEventsAsync(string userExternalId, int page = 1, int pageSize = 50);
}

public record EventSummaryDto(
    string ExternalId,
    string UserExternalId,
    decimal Profit,
    DateTime CreatedAt);