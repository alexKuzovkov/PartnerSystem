using PartnerSystem.Contracts;

namespace CommissionService.Application;

public interface ICommissionCalculator
{
    decimal CalculateCommission(decimal profit, int level, SchemaType schemaType);

    IReadOnlyList<CommissionCalculationResult> CalculateChainCommissions(
        decimal profit,
        IReadOnlyList<string> partnerChain,
        SchemaType schemaType);
}

public sealed record CommissionCalculationResult
{
    public required string PartnerExternalId { get; init; }
    public int Level { get; init; }
    public decimal Amount { get; init; }
    public SchemaType SchemaType { get; init; }
}
