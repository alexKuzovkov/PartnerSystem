using CommissionService.Application;
using Microsoft.AspNetCore.Mvc;
using PartnerSystem.Contracts;

namespace CommissionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AdminController : ControllerBase
{
    private readonly ISchemaSettingsService _schemaSettingsService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ISchemaSettingsService schemaSettingsService,
        ILogger<AdminController> logger)
    {
        _schemaSettingsService = schemaSettingsService;
        _logger = logger;
    }

    [HttpGet("schema")]
    public async Task<IActionResult> GetCurrentSchemaAsync(CancellationToken cancellationToken)
    {
        var schema = await _schemaSettingsService.GetCurrentSchemaAsync(cancellationToken);
        return Ok(new { schema = schema.ToString() });
    }

    [HttpPost("schema")]
    public async Task<IActionResult> SetSchemaAsync(
        [FromBody] SetSchemaRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Administrator is switching commission scheme to {Schema}",
            request.Schema);

        await _schemaSettingsService.SetCurrentSchemaAsync(request.Schema, cancellationToken);

        return Ok(new
        {
            message = "Commission scheme updated successfully",
            schema = request.Schema.ToString()
        });
    }
}

public sealed class SetSchemaRequest
{
    public SchemaType Schema { get; set; }
}
