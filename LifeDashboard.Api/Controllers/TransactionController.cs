using LifeDashboard.Api.Extensions;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

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
[Authorize]
[HttpGet]
public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions()
{
    try
    {
        var userId =
    User.GetUserId();


    var result =
        await _transactionService
        .GetAllTransactionsAsync(userId);

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
[Authorize]
[HttpPost]
public async Task<ActionResult<TransactionDto>> CreateTransaction(
    CreateTransactionDto dto)
{
    var userId =
        User.GetUserId();


    var result =
        await _transactionService
        .CreateTransactionAsync(
            userId,
            dto);


    return CreatedAtAction(
        nameof(GetTransactions),
        new { id = result.Id },
        result);
}

// DELETE: api/Transaction/{id}
[Authorize]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteTransaction(
    Guid id)
{
    var userId =
        User.GetUserId();


    await _transactionService
        .DeleteTransactionAsync(
            userId,
            id);


    return NoContent();
}
}
