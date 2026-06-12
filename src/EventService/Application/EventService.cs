using EventService.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EventService.Application;

public class EventService : IEventService
{
    private readonly EventDbContext _context;
    private readonly ILogger<EventService> _logger;

    public EventService(
        EventDbContext context,
        ILogger<EventService> logger)
    {
        _context = context;
        _logger = logger;
    }


    public async Task<List<EventSummaryDto>> GetUserEventsAsync(string userExternalId, int page = 1, int pageSize = 50)
    {
        return await _context.ProfitEvents
            .Where(e => e.UserExternalId == userExternalId)
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EventSummaryDto(
                e.EventExternalId,
                e.UserExternalId,
                e.Profit,
                e.CreatedAt))
            .ToListAsync();
    }
}