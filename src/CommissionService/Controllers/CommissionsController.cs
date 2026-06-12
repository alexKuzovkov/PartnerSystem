using CommissionService.Application;
using Microsoft.AspNetCore.Mvc;

namespace CommissionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommissionsController : ControllerBase
{
    private readonly ICommissionService _service;
    private readonly ILogger<CommissionsController> _logger;

    public CommissionsController(
        ICommissionService service,
        ILogger<CommissionsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("{eventExternalId}")]
    public async Task<ActionResult<CommissionDetailsDto>> GetEventDetails(string eventExternalId)
    {
        _logger.LogInformation("Запрос деталей комиссий для события {EventId}", eventExternalId);

        var details = await _service.GetCommissionDetails(eventExternalId);
        return Ok(details);
    }
}