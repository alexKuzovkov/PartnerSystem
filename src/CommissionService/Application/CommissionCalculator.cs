using PartnerSystem.Contracts;

namespace CommissionService.Application;

public class CommissionCalculator : ICommissionCalculator
{
    private const int MaxLevel = 10;

    private const decimal PercentageDivisor = 100m;

    public decimal CalculateCommission(decimal profit, int level, SchemaType schemaType)
    {
        if (profit <= 0)
            return 0m;

        if (level <= 0 || level > MaxLevel)
            return 0m;

        return schemaType switch
        {
            SchemaType.Linear => CalculateLinear(profit, level),
            SchemaType.Fibonacci => CalculateFibonacci(profit, level),
            _ => throw new ArgumentOutOfRangeException(nameof(schemaType), schemaType, null)
        };
    }

    public List<CommissionCalculationResult> CalculateChainCommissions(
        decimal profit,
        List<string> partnerChain,
        SchemaType schemaType)
    {

        if (profit <= 0 || partnerChain == null || partnerChain.Count == 0)
            return new List<CommissionCalculationResult>();

        return partnerChain
            .Take(MaxLevel)
            .Select((partnerId, index) => new
            {
                PartnerId = partnerId,
                Level = index + 1,
                Amount = CalculateCommission(profit, index + 1, schemaType)
            })
            .Where(x => x.Amount > 0)
            .Select(x => new CommissionCalculationResult
            {
                PartnerExternalId = x.PartnerId,
                Level = x.Level,
                Amount = x.Amount,
                SchemaType = schemaType
            })
            .ToList();
    }

    private static decimal CalculateLinear(decimal profit, int level)
        => level * profit / PercentageDivisor;

    private static decimal CalculateFibonacci(decimal profit, int level)
        => GetFibonacciNumber(level) * profit / PercentageDivisor;

    private static decimal GetFibonacciNumber(int n)
    {
        if (n <= 0) return 0m;
        if (n <= 2) return 1m;

        decimal a = 1m, b = 1m;
        for (int i = 3; i <= n; i++)
            (a, b) = (b, a + b);

        return b;
    }
}