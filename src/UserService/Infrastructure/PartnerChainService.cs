using Grpc.Core;
using PartnerSystem.Contracts.Grpc;
using UserService.Application;

namespace UserService.Services;

public sealed class PartnerChainService : PartnerService.PartnerServiceBase
{
    private readonly IUserService _userService;
    private readonly ILogger<PartnerChainService> _logger;

    public PartnerChainService(
        IUserService userService,
        ILogger<PartnerChainService> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public override async Task<GetPartnerChainResponse> GetPartnerChain(
        GetPartnerChainRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "gRPC GetPartnerChain requested for {UserId}",
            request.UserExternalId);

        var chain = await _userService.GetPartnerChainAsync(
            request.UserExternalId,
            request.MaxLevels,
            context.CancellationToken);

        var response = new GetPartnerChainResponse();
        response.PartnerExternalIds.AddRange(chain.Select(c => c.ExternalId));
        return response;
    }

    public override async Task<AddUserResponse> AddUser(
        AddUserRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC AddUser requested for {UserId}", request.ExternalId);

        var result = await _userService.CreateUserAsync(
            request.ExternalId,
            string.IsNullOrWhiteSpace(request.ParentExternalId) ? null : request.ParentExternalId,
            context.CancellationToken);

        return new AddUserResponse
        {
            Success = result.Success,
            Message = result.Message
        };
    }

    public override async Task<SetPartnerLinkResponse> SetPartnerLink(
        SetPartnerLinkRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "gRPC SetPartnerLink requested: {UserId} -> {PartnerId}",
            request.UserExternalId,
            request.PartnerExternalId);

        var result = await _userService.SetPartnerLinkAsync(
            request.UserExternalId,
            request.PartnerExternalId,
            context.CancellationToken);

        return new SetPartnerLinkResponse
        {
            Success = result.Success,
            Message = result.Message
        };
    }

    public override async Task<GetDownlineResponse> GetDownline(
        GetDownlineRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "gRPC GetDownline requested for {UserId}, MaxLevels={MaxLevels}",
            request.UserExternalId,
            request.MaxLevels);

        var nodes = await _userService.GetDownlineAsync(
            request.UserExternalId,
            context.CancellationToken);

        var maxLevels = Math.Clamp(request.MaxLevels, 0, 10);
        var response = new GetDownlineResponse();

        foreach (var node in nodes.Where(n => n.Level <= maxLevels))
        {
            response.Nodes.Add(new PartnerSystem.Contracts.Grpc.DownlineNode
            {
                UserExternalId = node.UserExternalId,
                Level = node.Level,
                DirectReferralsCount = node.DirectReferralsCount
            });
        }

        _logger.LogInformation(
            "gRPC GetDownline returned {Count} nodes for {UserId}",
            response.Nodes.Count,
            request.UserExternalId);

        return response;
    }
}
