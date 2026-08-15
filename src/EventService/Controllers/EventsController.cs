using EventService.Application;
using Microsoft.AspNetCore.Mvc;

namespace EventService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class EventsController : ControllerBase
{
    private readonly IEventProcessor _processor;
    private readonly IEventService _eventService;

    public EventsController(
        IEventProcessor processor,
        IEventService eventService)
    {
        _processor = processor;
        _eventService = eventService;
    }

    [HttpPost]
    public async Task<ActionResult> PostEventAsync(
        [FromBody] ProfitEventRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _processor.ProcessProfitEventAsync(
            new ProfitEventDto
            {
                EventExternalId = request.EventExternalId,
                UserExternalId = request.UserExternalId,
                Profit = request.Profit,
                OccurredAt = request.OccurredAt
            },
            cancellationToken);

        if (!result.Success)
            return Conflict(new { message = result.Message });

        return Accepted(new { message = "Event accepted" });
    }

    [HttpGet]
    public async Task<IActionResult> GetUserEventsAsync(
        [FromQuery] string userExternalId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var events = await _eventService.GetUserEventsAsync(
            userExternalId,
            page,
            pageSize,
            cancellationToken);
        return Ok(events);
    }
}
