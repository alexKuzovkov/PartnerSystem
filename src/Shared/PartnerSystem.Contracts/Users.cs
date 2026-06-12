namespace PartnerSystem.Contracts;

/// <summary>
/// DTO пользователя
/// </summary>
public record UserDto(
    string ExternalId,
    string? ParentExternalId,
    DateTime CreatedAt
);

/// <summary>
/// Запрос на создание пользователя
/// </summary>
public record CreateUserRequest(
    string ExternalId,
    string? ParentExternalId
);

/// <summary>
/// Информация о партнерской цепочке (вверх)
/// </summary>
public record PartnerChainItem(
    string ExternalId,
    int Level
);