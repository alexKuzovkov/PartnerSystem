using Microsoft.AspNetCore.Mvc;
using WalletService.Application;

namespace WalletService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WalletsController : ControllerBase
{
    private readonly IWalletProcessor _processor;

    public WalletsController(IWalletProcessor processor)
    {
        _processor = processor;
    }

    [HttpGet("{userExternalId}/balance")]
    public async Task<ActionResult<BalanceDto>> GetBalanceAsync(
        string userExternalId,
        CancellationToken cancellationToken)
    {
        var balance = await _processor.GetBalanceAsync(userExternalId, cancellationToken);
        return Ok(new BalanceDto(userExternalId, balance));
    }

    [HttpGet("{userExternalId}/transactions")]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactionsAsync(
        string userExternalId,
        CancellationToken cancellationToken)
    {
        var transactions = await _processor.GetTransactionHistoryAsync(userExternalId, cancellationToken);
        var dtos = transactions.Select(t => new TransactionDto(
            t.Id,
            t.Amount,
            t.CommissionEventExternalId,
            t.CreatedAt)).ToList();

        return Ok(dtos);
    }
}

public record BalanceDto(string UserExternalId, decimal Balance);
public record TransactionDto(Guid Id, decimal Amount, string CommissionEventExternalId, DateTime CreatedAt);
