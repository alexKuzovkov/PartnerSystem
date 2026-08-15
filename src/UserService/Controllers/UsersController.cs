using Microsoft.AspNetCore.Mvc;
using PartnerSystem.Contracts;
using UserService.Application;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUserAsync(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.CreateUserAsync(
            request.ExternalId,
            request.ParentExternalId,
            cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return CreatedAtAction(
            nameof(GetPartnerChainAsync),
            new { userExternalId = request.ExternalId },
            new UserDto(request.ExternalId, request.ParentExternalId, DateTime.UtcNow));
    }

    [HttpPost("{userExternalId}/partner")]
    public async Task<IActionResult> SetPartnerAsync(
        string userExternalId,
        [FromBody] SetPartnerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.SetPartnerLinkAsync(
            userExternalId,
            request.PartnerExternalId,
            cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpGet("{userExternalId}/chain")]
    public async Task<ActionResult<List<PartnerChainItem>>> GetPartnerChainAsync(
        string userExternalId,
        [FromQuery] int maxLevels = 10,
        CancellationToken cancellationToken = default)
    {
        var chain = await _userService.GetPartnerChainAsync(
            userExternalId,
            maxLevels,
            cancellationToken);
        return Ok(chain);
    }

    [HttpGet("{userExternalId}/downline")]
    public async Task<IActionResult> GetDownlineAsync(
        string userExternalId,
        CancellationToken cancellationToken)
    {
        var tree = await _userService.GetDownlineAsync(userExternalId, cancellationToken);
        return Ok(tree);
    }
}

public record SetPartnerRequest(string PartnerExternalId);
