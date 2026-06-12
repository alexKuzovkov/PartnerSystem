using Microsoft.AspNetCore.Mvc;
using WalletService.Application;

namespace WalletService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletsController : ControllerBase
{
    private readonly IWalletProcessor _processor;
    private readonly ILogger<WalletsController> _logger;

    public WalletsController(
        IWalletProcessor processor,
        ILogger<WalletsController> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    [HttpGet("{userExternalId}/balance")]
    public async Task<ActionResult<BalanceDto>> GetBalance(string userExternalId, CancellationToken cancellationToken)
    {
        var balance = await _processor.GetBalance(userExternalId, cancellationToken);
        return Ok(new BalanceDto(userExternalId, balance));
    }

    [HttpGet("{userExternalId}/transactions")]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions(string userExternalId, CancellationToken cancellationToken)
    {
        var transactions = await _processor.GetTransactionHistory(userExternalId, cancellationToken);
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