using Grpc.Core;
using PartnerSystem.Contracts.Grpc;
using UserService.Application;

namespace UserService.Services;

public class PartnerChainService : PartnerService.PartnerServiceBase
{
    private readonly IUserService _userService;
    private readonly ILogger<PartnerChainService> _logger;

    public PartnerChainService(IUserService userService, ILogger<PartnerChainService> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public override async Task<GetPartnerChainResponse> GetPartnerChain(
        GetPartnerChainRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("gRPC: GetPartnerChain для {UserId}", request.UserExternalId);

        var chain = await _userService.GetPartnerChainAsync(request.UserExternalId, request.MaxLevels);

        var response = new GetPartnerChainResponse();
        response.PartnerExternalIds.AddRange(chain.Select(c => c.ExternalId));

        return response;
    }

    public override async Task<AddUserResponse> AddUser(AddUserRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC: AddUser {UserId}", request.ExternalId);

        var result = await _userService.CreateUserAsync(request.ExternalId, request.ParentExternalId);

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
        _logger.LogInformation("gRPC: SetPartnerLink {UserId} -> {PartnerId}",
            request.UserExternalId, request.PartnerExternalId);

        var result = await _userService.SetPartnerLinkAsync(request.UserExternalId, request.PartnerExternalId);

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
            "gRPC: Запрос downline для {UserId}, MaxLevels={MaxLevels}",
            request.UserExternalId, request.MaxLevels);

        var response = new GetDownlineResponse();
        var maxLevels = Math.Min(request.MaxLevels, 10);

        var nodes = await _userService.GetDownlineAsync(request.UserExternalId);

        if (nodes == null)
        {
            _logger.LogWarning("Пользователь {UserId} не найден", request.UserExternalId);
            return response;
        }

        foreach (var node in nodes)
        {
            response.Nodes.Add(new PartnerSystem.Contracts.Grpc.DownlineNode
            {
                UserExternalId = node.UserExternalId,
                Level = node.Level,
                DirectReferralsCount = node.DirectReferralsCount
            });
        }

        _logger.LogInformation(
            "gRPC: Возвращено {Count} узлов downline для {UserId}",
            response.Nodes.Count, request.UserExternalId);

        return response;
    }
}