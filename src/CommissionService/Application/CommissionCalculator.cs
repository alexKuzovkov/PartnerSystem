using PartnerSystem.Contracts;

namespace CommissionService.Application;

public sealed class CommissionCalculator : ICommissionCalculator
{
    public const int MaximumLevel = 10;
    private const decimal PercentageDivisor = 100m;

    public decimal CalculateCommission(decimal profit, int level, SchemaType schemaType)
    {
        if (profit <= 0 || level is < 1 or > MaximumLevel)
            return 0m;

        return schemaType switch
        {
            SchemaType.Linear => level * profit / PercentageDivisor,
            SchemaType.Fibonacci => GetFibonacciNumber(level) * profit / PercentageDivisor,
            _ => throw new ArgumentOutOfRangeException(nameof(schemaType), schemaType, "Unsupported commission scheme.")
        };
    }

    public IReadOnlyList<CommissionCalculationResult> CalculateChainCommissions(
        decimal profit,
        IReadOnlyList<string> partnerChain,
        SchemaType schemaType)
    {
        ArgumentNullException.ThrowIfNull(partnerChain);

        if (profit <= 0 || partnerChain.Count == 0)
            return [];

        return partnerChain
            .Take(MaximumLevel)
            .Select((partnerId, index) => new CommissionCalculationResult
            {
                PartnerExternalId = partnerId,
                Level = index + 1,
                Amount = CalculateCommission(profit, index + 1, schemaType),
                SchemaType = schemaType
            })
            .Where(result => result.Amount > 0)
            .ToList();
    }

    private static decimal GetFibonacciNumber(int n)
    {
        if (n <= 0)
            return 0m;
        if (n <= 2)
            return 1m;

        decimal previous = 1m;
        decimal current = 1m;

        for (var index = 3; index <= n; index++)
            (previous, current) = (current, previous + current);

        return current;
    }
}
