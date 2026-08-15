using PartnerSystem.Contracts;

namespace UserService.Application;

public interface IUserService
{
    Task<UserOperationResult> CreateUserAsync(
        string externalId,
        string? parentExternalId,
        CancellationToken cancellationToken = default);

    Task<UserOperationResult> SetPartnerLinkAsync(
        string userExternalId,
        string partnerExternalId,
        CancellationToken cancellationToken = default);

    Task<List<PartnerChainItem>> GetPartnerChainAsync(
        string userExternalId,
        int maxLevels = 10,
        CancellationToken cancellationToken = default);

    Task<List<DownlineNode>> GetDownlineAsync(
        string userExternalId,
        CancellationToken cancellationToken = default);
}

public record UserOperationResult(bool Success, string Message);

public record DownlineNode(
    string UserExternalId,
    int Level,
    int DirectReferralsCount);
