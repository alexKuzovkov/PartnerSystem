namespace PartnerSystem.Contracts;

/// <summary>
/// User API contract.
/// </summary>
public record UserDto(
    string ExternalId,
    string? ParentExternalId,
    DateTime CreatedAt);

/// <summary>
/// Request used to create a user in the partner hierarchy.
/// </summary>
public record CreateUserRequest(
    string ExternalId,
    string? ParentExternalId);

/// <summary>
/// One item in the upward partner chain.
/// </summary>
public record PartnerChainItem(
    string ExternalId,
    int Level);
