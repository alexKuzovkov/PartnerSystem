namespace PartnerSystem.Contracts;

/// <summary>
/// Represents a user's profit or loss event.
/// </summary>
public record ProfitEvent(
    string EventExternalId,
    string UserExternalId,
    decimal Profit,
    DateTime OccurredAt);

/// <summary>
/// Requests commission calculation for a profit event.
/// </summary>
public record CommissionCalculationRequested(
    string EventExternalId,
    string UserExternalId,
    decimal Profit,
    DateTime OccurredAt);

/// <summary>
/// Defines the supported commission calculation schemes.
/// </summary>
public enum SchemaType
{
    Linear = 0,
    Fibonacci = 1
}

/// <summary>
/// Represents a calculated commission for one hierarchy level.
/// </summary>
public record CommissionCalculated(
    string EventExternalId,
    string PartnerExternalId,
    int Level,
    decimal Amount,
    SchemaType SchemaType);
