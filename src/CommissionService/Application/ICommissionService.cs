using PartnerSystem.Contracts;

namespace CommissionService.Application;

public interface ICommissionService
{
    Task ProcessCommissionCalculation(CommissionCalculationRequest request);
    Task<CommissionDetailsDto> GetCommissionDetails(string eventExternalId);
}

public record CommissionCalculationRequest
{
    public string EventExternalId { get; init; } = null!;
    public string UserExternalId { get; init; } = null!;
    public decimal Profit { get; init; }
    public DateTime OccurredAt { get; init; }
    public SchemaType SchemaType { get; init; }
}

public record CommissionDetailsDto
{
    public string EventExternalId { get; init; } = null!;
    public List<CommissionItemDto> Commissions { get; init; } = new();
}

public record CommissionItemDto
{
    public string PartnerExternalId { get; init; } = null!;
    public int Level { get; init; }
    public decimal Amount { get; init; }
    public SchemaType SchemaType { get; init; }
    public bool IsPaid { get; init; }
}