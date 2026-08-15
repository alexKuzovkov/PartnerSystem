using EventService.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EventService.Application;

public sealed class EventService : IEventService
{
    private const int MaximumPageSize = 200;
    private readonly EventDbContext _context;

    public EventService(EventDbContext context)
    {
        _context = context;
    }

    public Task<List<EventSummaryDto>> GetUserEventsAsync(
        string userExternalId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaximumPageSize);

        return _context.ProfitEvents
            .AsNoTracking()
            .Where(e => e.UserExternalId == userExternalId)
            .OrderByDescending(e => e.CreatedAt)
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(e => new EventSummaryDto(
                e.EventExternalId,
                e.UserExternalId,
                e.Profit,
                e.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
