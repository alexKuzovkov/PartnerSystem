using CommissionService.Application;
using Microsoft.AspNetCore.Mvc;
using PartnerSystem.Contracts;

namespace CommissionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
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
    public async Task<IActionResult> GetCurrentSchema(CancellationToken cancellationToken)
    {
        var schema = await _schemaSettingsService.GetCurrentSchemaAsync();
        return Ok(new { schema = schema.ToString() });
    }

    [HttpPost("schema")]
    public async Task<IActionResult> SetSchema([FromBody] SetSchemaRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Администратор переключает схему на {Schema}",
            request.Schema);

        await _schemaSettingsService.SetCurrentSchemaAsync(request.Schema, cancellationToken);

        return Ok(new
        {
            message = "Схема успешно переключена",
            schema = request.Schema.ToString()
        });
    }
}

public class SetSchemaRequest
{
    public SchemaType Schema { get; set; }
}