using Microsoft.AspNetCore.Mvc;
using PartnerSystem.Contracts;
using UserService.Application;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUserAsync([FromBody] CreateUserRequest request)
    {
        var result = await _userService.CreateUserAsync(request.ExternalId, request.ParentExternalId);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new UserDto(request.ExternalId, request.ParentExternalId, DateTime.UtcNow));
    }

    [HttpPost("{userExternalId}/partner")]
    public async Task<IActionResult> SetPartnerAsync(string userExternalId, [FromBody] SetPartnerRequest request, CancellationToken cancellationToken)
    {
        var result = await _userService.SetPartnerLinkAsync(userExternalId, request.PartnerExternalId);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpGet("{userExternalId}/chain")]
    public async Task<ActionResult<List<PartnerChainItem>>> GetPartnerChainAsync(
        string userExternalId,
        [FromQuery] int maxLevels = 10)
    {
        var chain = await _userService.GetPartnerChainAsync(userExternalId, maxLevels);
        return Ok(chain);
    }

    [HttpGet("{userExternalId}/downline")]
    public async Task<IActionResult> GetDownline(string userExternalId)
    {
        var tree = await _userService.GetDownlineAsync(userExternalId);
        return Ok(tree);
    }
}

public record SetPartnerRequest(string PartnerExternalId);