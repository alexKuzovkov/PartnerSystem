using CommissionService.Application;
using Microsoft.AspNetCore.Mvc;

namespace CommissionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CommissionsController : ControllerBase
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
    public async Task<ActionResult<CommissionDetailsDto>> GetEventDetailsAsync(
        string eventExternalId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Commission details requested for event {EventId}",
            eventExternalId);

        var details = await _service.GetCommissionDetailsAsync(eventExternalId, cancellationToken);
        return Ok(details);
    }
}
