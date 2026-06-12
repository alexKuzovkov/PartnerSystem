using PartnerSystem.Contracts;

namespace CommissionService.Application;

public interface ICommissionCalculator
{
    decimal CalculateCommission(decimal profit, int level, SchemaType schemaType);
    List<CommissionCalculationResult> CalculateChainCommissions(
        decimal profit,
        List<string> partnerChain,
        SchemaType schemaType);
}

public record CommissionCalculationResult
{
    public string PartnerExternalId { get; init; } = null!;
    public int Level { get; init; }
    public decimal Amount { get; init; }
    public SchemaType SchemaType { get; init; }
}