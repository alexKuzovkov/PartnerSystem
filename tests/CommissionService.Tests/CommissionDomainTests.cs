using CommissionService.Domain;
using FluentAssertions;
using PartnerSystem.Contracts;
using Xunit;

namespace CommissionService.Tests;

public sealed class CommissionDomainTests
{
    [Fact]
    public void Constructor_ValidValues_CreatesCommission()
    {
        var commission = new Commission("evt-1", "partner-1", 2, 25m, SchemaType.Linear);

        commission.EventExternalId.Should().Be("evt-1");
        commission.PartnerExternalId.Should().Be("partner-1");
        commission.Level.Should().Be(2);
        commission.Amount.Should().Be(25m);
        commission.SchemaType.Should().Be(SchemaType.Linear);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Constructor_InvalidLevel_Throws(int level)
    {
        var action = () => new Commission("evt-1", "partner-1", level, 25m, SchemaType.Linear);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NonPositiveAmount_Throws()
    {
        var action = () => new Commission("evt-1", "partner-1", 1, 0m, SchemaType.Linear);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
