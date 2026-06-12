using PartnerSystem.Contracts;

namespace UserService.Application;

public interface IUserService
{
    Task<UserOperationResult> CreateUserAsync(string externalId, string? parentExternalId);
    Task<UserOperationResult> SetPartnerLinkAsync(string userExternalId, string partnerExternalId);
    Task<List<PartnerChainItem>> GetPartnerChainAsync(string userExternalId, int maxLevels = 10);
    Task<List<DownlineNode>> GetDownlineAsync(string userExternalId);
}

public record UserOperationResult(bool Success, string Message);
public record DownlineNode(
    string UserExternalId,
    int Level,
    int DirectReferralsCount);