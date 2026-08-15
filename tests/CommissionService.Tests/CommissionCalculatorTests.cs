using CommissionService.Application;
using FluentAssertions;
using PartnerSystem.Contracts;
using Xunit;

namespace CommissionService.Tests;

public sealed class CommissionCalculatorTests
{
    private readonly ICommissionCalculator _calculator = new CommissionCalculator();

    #region Linear scheme

    [Theory]
    [InlineData(1000, 1, 10)]
    [InlineData(1000, 2, 20)]
    [InlineData(1000, 3, 30)]
    [InlineData(1000, 5, 50)]
    [InlineData(1000, 10, 100)]
    public void CalculateCommission_LinearScheme_ReturnsExpectedAmount(
        decimal profit,
        int level,
        decimal expected)
    {
        var result = _calculator.CalculateCommission(profit, level, SchemaType.Linear);
        result.Should().Be(expected);
    }

    #endregion

    #region Fibonacci scheme

    [Theory]
    [InlineData(1000, 1, 10)]
    [InlineData(1000, 2, 10)]
    [InlineData(1000, 3, 20)]
    [InlineData(1000, 4, 30)]
    [InlineData(1000, 5, 50)]
    [InlineData(1000, 6, 80)]
    [InlineData(1000, 7, 130)]
    [InlineData(1000, 8, 210)]
    [InlineData(1000, 9, 340)]
    [InlineData(1000, 10, 550)]
    public void CalculateCommission_FibonacciScheme_ReturnsExpectedAmount(
        decimal profit,
        int level,
        decimal expected)
    {
        var result = _calculator.CalculateCommission(profit, level, SchemaType.Fibonacci);
        result.Should().Be(expected);
    }

    #endregion

    #region Boundary conditions

    [Theory]
    [InlineData(0, 1, SchemaType.Linear)]
    [InlineData(-100, 1, SchemaType.Linear)]
    [InlineData(-500, 3, SchemaType.Fibonacci)]
    [InlineData(1000, 0, SchemaType.Linear)]
    [InlineData(1000, 11, SchemaType.Linear)]
    [InlineData(1000, 15, SchemaType.Fibonacci)]
    public void CalculateCommission_InvalidInput_ReturnsZero(
        decimal profit,
        int level,
        SchemaType schema)
    {
        _calculator.CalculateCommission(profit, level, schema).Should().Be(0m);
    }

    [Fact]
    public void CalculateCommission_UnknownScheme_Throws()
    {
        var action = () => _calculator.CalculateCommission(1000m, 1, (SchemaType)999);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region Partner chain

    [Fact]
    public void CalculateChainCommissions_LinearScheme_CalculatesEveryLevel()
    {
        var partnerChain = new List<string> { "p1", "p2", "p3" };

        var results = _calculator.CalculateChainCommissions(
            1000m,
            partnerChain,
            SchemaType.Linear);

        results.Should().HaveCount(3);
        results[0].Should().BeEquivalentTo(new { PartnerExternalId = "p1", Level = 1, Amount = 10m });
        results[1].Should().BeEquivalentTo(new { PartnerExternalId = "p2", Level = 2, Amount = 20m });
        results[2].Should().BeEquivalentTo(new { PartnerExternalId = "p3", Level = 3, Amount = 30m });
    }

    [Fact]
    public void CalculateChainCommissions_FibonacciScheme_CalculatesEveryLevel()
    {
        var partnerChain = new List<string> { "p1", "p2", "p3", "p4", "p5" };

        var results = _calculator.CalculateChainCommissions(
            1000m,
            partnerChain,
            SchemaType.Fibonacci);

        results.Select(x => x.Amount).Should().Equal(10m, 10m, 20m, 30m, 50m);
        results.Should().OnlyContain(x => x.SchemaType == SchemaType.Fibonacci);
    }

    [Fact]
    public void CalculateChainCommissions_MoreThanTenPartners_TruncatesAtMaximumLevel()
    {
        var partnerChain = Enumerable.Range(1, 15).Select(i => $"partner{i}").ToList();

        var results = _calculator.CalculateChainCommissions(
            1000m,
            partnerChain,
            SchemaType.Linear);

        results.Should().HaveCount(CommissionCalculator.MaximumLevel);
        results.Max(r => r.Level).Should().Be(CommissionCalculator.MaximumLevel);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void CalculateChainCommissions_NonPositiveProfit_ReturnsEmpty(decimal profit)
    {
        _calculator.CalculateChainCommissions(
                profit,
                new List<string> { "p1", "p2" },
                SchemaType.Linear)
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void CalculateChainCommissions_EmptyChain_ReturnsEmpty()
    {
        _calculator.CalculateChainCommissions(
                1000m,
                Array.Empty<string>(),
                SchemaType.Linear)
            .Should()
            .BeEmpty();
    }

    #endregion
}
