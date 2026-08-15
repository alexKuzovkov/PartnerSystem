using FluentAssertions;
using WalletService.Domain;
using Xunit;

namespace WalletService.Tests;

public sealed class PendingPayoutTests
{
    [Fact]
    public void LockForProcessing_SetsClaimMetadata()
    {
        var payout = new PendingPayout("evt-1", "partner-1", 10m, 1);

        payout.LockForProcessing("instance-a");

        payout.LockedByInstance.Should().Be("instance-a");
        payout.LockedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsPaid_ClearsClaimAndSetsPaidState()
    {
        var payout = new PendingPayout("evt-1", "partner-1", 10m, 1);
        payout.LockForProcessing("instance-a");

        payout.MarkAsPaid();

        payout.IsPaid.Should().BeTrue();
        payout.PaidAt.Should().NotBeNull();
        payout.LockedByInstance.Should().BeNull();
        payout.LockedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Constructor_InvalidLevel_Throws(int level)
    {
        var action = () => new PendingPayout("evt-1", "partner-1", 10m, level);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
