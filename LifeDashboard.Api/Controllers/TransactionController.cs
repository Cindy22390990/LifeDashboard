using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LifeDashboard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionController : ControllerBase
{
private readonly ITransactionService _transactionService;

public TransactionController(ITransactionService transactionService)
{
    _transactionService = transactionService;
}

// GET: api/Transaction
[HttpGet]
public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions()
{
    try
    {
        var result = await _transactionService.GetAllTransactionsAsync();

        return Ok(result);
    }
    catch(Exception ex)
    {
        Console.WriteLine(ex);

        return StatusCode(500, new
        {
            Message = ex.Message,
            Detail = ex.InnerException?.Message
        });
    }
}

// POST: api/Transaction
[HttpPost]
public async Task<ActionResult<TransactionDto>> CreateTransaction(CreateTransactionDto dto)
{
    var result = await _transactionService.CreateTransactionAsync(dto);
    return CreatedAtAction(nameof(GetTransactions), new { id = result.Id }, result);
}

// DELETE: api/Transaction/{id}
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteTransaction(Guid id)
{
    await _transactionService.DeleteTransactionAsync(id);
    return NoContent();
}
}
