using CommissionService.Application;
using FluentAssertions;
using PartnerSystem.Contracts;
using Xunit;

namespace CommissionService.Tests;

public class CommissionCalculatorTests
{
    private readonly ICommissionCalculator _calculator;

    public CommissionCalculatorTests()
    {
        _calculator = new CommissionCalculator();
    }

    #region Linear схема — одиночный расчет

    [Theory]
    [InlineData(1000, 1, 10)]
    [InlineData(1000, 2, 20)]
    [InlineData(1000, 3, 30)]
    [InlineData(1000, 5, 50)]
    [InlineData(1000, 10, 100)]
    public void CalculateCommission_Linear_ShouldBeCorrect(
        decimal profit, int level, decimal expected)
    {
        var result = _calculator.CalculateCommission(profit, level, SchemaType.Linear);
        result.Should().Be(expected);
    }

    #endregion

    #region Fibonacci схема — одиночный расчет

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
    public void CalculateCommission_Fibonacci_ShouldBeCorrect(
        decimal profit, int level, decimal expected)
    {
        var result = _calculator.CalculateCommission(profit, level, SchemaType.Fibonacci);
        result.Should().Be(expected);
    }

    #endregion

    #region Граничные случаи — одиночный расчет


    [Theory]
    [InlineData(0, 1, SchemaType.Linear)] 
    [InlineData(-100, 1, SchemaType.Linear)] 
    [InlineData(-500, 3, SchemaType.Fibonacci)] 
    [InlineData(1000, 0, SchemaType.Linear)]
    [InlineData(1000, 11, SchemaType.Linear)]
    [InlineData(1000, 15, SchemaType.Fibonacci)]
    public void CalculateCommission_InvalidInput_ReturnsZero(
        decimal profit, int level, SchemaType schema)
    {
        var result = _calculator.CalculateCommission(profit, level, schema);
        result.Should().Be(0);
    }

    #endregion

    #region Расчет для цепочки — общие случаи

    [Fact]
    public void CalculateChainCommissions_Linear_ShouldCalculateForAllLevels()
    {
        var partnerChain = new List<string> { "p1", "p2", "p3" };
        var results = _calculator.CalculateChainCommissions(1000, partnerChain, SchemaType.Linear);

        results.Should().HaveCount(3);
        results[0].Should().BeEquivalentTo(new { PartnerExternalId = "p1", Level = 1, Amount = 10m });
        results[1].Should().BeEquivalentTo(new { PartnerExternalId = "p2", Level = 2, Amount = 20m });
        results[2].Should().BeEquivalentTo(new { PartnerExternalId = "p3", Level = 3, Amount = 30m });
    }

    [Fact]
    public void CalculateChainCommissions_Fibonacci_ShouldCalculateForAllLevels()
    {
        var partnerChain = new List<string> { "p1", "p2", "p3", "p4", "p5" };
        var results = _calculator.CalculateChainCommissions(1000, partnerChain, SchemaType.Fibonacci);

        results.Should().HaveCount(5);
        results[0].Amount.Should().Be(10m);
        results[1].Amount.Should().Be(10m);
        results[2].Amount.Should().Be(20m);
        results[3].Amount.Should().Be(30m); 
        results[4].Amount.Should().Be(50m); 
    }

    [Fact]
    public void CalculateChainCommissions_ShouldStoreSchemaType()
    {
        var partnerChain = new List<string> { "p1" };

        var linearResults = _calculator.CalculateChainCommissions(1000, partnerChain, SchemaType.Linear);
        var fibResults = _calculator.CalculateChainCommissions(1000, partnerChain, SchemaType.Fibonacci);

        linearResults[0].SchemaType.Should().Be(SchemaType.Linear);
        fibResults[0].SchemaType.Should().Be(SchemaType.Fibonacci);
    }

    #endregion

    #region Расчет для цепочки — граничные случаи

    [Fact]
    public void CalculateChainCommissions_ShouldRespectMaxLevel()
    {
        var partnerChain = Enumerable.Range(1, 15).Select(i => $"partner{i}").ToList();
        var results = _calculator.CalculateChainCommissions(1000, partnerChain, SchemaType.Linear);

        results.Should().HaveCount(10);
        results.Max(r => r.Level).Should().Be(10);
    }

    [Fact]
    public void CalculateChainCommissions_ShouldReturnEmptyForZeroProfit()
    {
        var results = _calculator.CalculateChainCommissions(
            0, new List<string> { "p1" }, SchemaType.Linear);
        results.Should().BeEmpty();
    }

    [Fact]
    public void CalculateChainCommissions_ShouldReturnEmptyForNegativeProfit()
    {
        var results = _calculator.CalculateChainCommissions(
            -100, new List<string> { "p1", "p2" }, SchemaType.Linear);
        results.Should().BeEmpty();
    }

    [Fact]
    public void CalculateChainCommissions_ShouldReturnEmptyForEmptyChain()
    {
        var results = _calculator.CalculateChainCommissions(
            1000, new List<string>(), SchemaType.Linear);
        results.Should().BeEmpty();
    }

    #endregion
}