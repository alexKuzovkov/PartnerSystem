using PartnerSystem.Contracts;

namespace CommissionService.Application;

public interface ICommissionService
{
    Task ProcessCommissionCalculationAsync(
        CommissionCalculationRequest request,
        CancellationToken cancellationToken = default);

    Task<CommissionDetailsDto> GetCommissionDetailsAsync(
        string eventExternalId,
        CancellationToken cancellationToken = default);
}

public record CommissionCalculationRequest
{
    public required string EventExternalId { get; init; }
    public required string UserExternalId { get; init; }
    public decimal Profit { get; init; }
    public DateTime OccurredAt { get; init; }
    public SchemaType SchemaType { get; init; }
}

public record CommissionDetailsDto
{
    public required string EventExternalId { get; init; }
    public List<CommissionItemDto> Commissions { get; init; } = [];
}

public record CommissionItemDto
{
    public required string PartnerExternalId { get; init; }
    public int Level { get; init; }
    public decimal Amount { get; init; }
    public SchemaType SchemaType { get; init; }
}
