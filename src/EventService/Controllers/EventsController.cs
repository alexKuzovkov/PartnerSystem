using EventService.Application;
using Microsoft.AspNetCore.Mvc;

namespace EventService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventProcessor _processor;
    private readonly IEventService _eventService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventProcessor processor,
        IEventService eventService,
        ILogger<EventsController> logger)
    {
        _processor = processor;
        _eventService = eventService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult> PostEventAsync([FromBody] ProfitEventRequest request, CancellationToken cancellationToken)
    {
        var eventDto = new ProfitEventDto
        {
            EventExternalId = request.EventExternalId,
            UserExternalId = request.UserExternalId,
            Profit = request.Profit,
            OccurredAt = request.OccurredAt
        };

        var result = await _processor.ProcessProfitEvent(eventDto);

        if (!result.Success)
            return Conflict(new { message = result.Message });

        return Ok(new { message = "Event accepted" });
    }

    [HttpGet]
    public async Task<IActionResult> GetUserEvents(
    [FromQuery] string userExternalId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50)
    {
        var events = await _eventService.GetUserEventsAsync(userExternalId, page, pageSize);
        return Ok(events);
    }
}