using FluentAssertions;
using WalletService.Domain;
using Xunit;

namespace WalletService.Tests;

public sealed class WalletTests
{
    [Fact]
    public void AddBalance_PositiveAmount_IncreasesBalance()
    {
        var wallet = new Wallet("user-1");

        wallet.AddBalance(42.50m);

        wallet.Balance.Should().Be(42.50m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddBalance_NonPositiveAmount_Throws(decimal amount)
    {
        var wallet = new Wallet("user-1");
        var action = () => wallet.AddBalance(amount);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Withdraw_SufficientBalance_DecreasesBalance()
    {
        var wallet = new Wallet("user-1");
        wallet.AddBalance(100m);

        wallet.Withdraw(35m);

        wallet.Balance.Should().Be(65m);
    }

    [Fact]
    public void Withdraw_InsufficientBalance_Throws()
    {
        var wallet = new Wallet("user-1");
        wallet.AddBalance(10m);

        var action = () => wallet.Withdraw(11m);

        action.Should().Throw<InvalidOperationException>();
        wallet.Balance.Should().Be(10m);
    }
}
