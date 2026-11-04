using LifeDashboard.Api.Extensions;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LifeDashboard.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

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
    try
    {
        var userId = User.GetUserId();
        var result = await _transactionService.CreateTransactionAsync(userId, dto);

        return CreatedAtAction(
            nameof(GetTransactions),
            new { id = result.Id },
            result);
    }
    catch (InsufficientFundsException ex)
    {
        return Conflict(new { Message = ex.Message });
    }
    catch (DbUpdateConcurrencyException)
    {
        return Conflict(new
        {
            Message = "帳戶資料已由其他操作更新，請重新載入後再試。"
        });
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { Message = ex.Message });
    }
}

// DELETE: api/Transaction/{id}
[Authorize]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteTransaction(
    Guid id)
{
    var userId =
        User.GetUserId();


    try
    {
        await _transactionService
            .DeleteTransactionAsync(
                userId,
                id);

        return NoContent();
    }
    catch (InsufficientFundsException ex)
    {
        return Conflict(new { Message = ex.Message });
    }
    catch (DbUpdateConcurrencyException)
    {
        return Conflict(new
        {
            Message = "帳戶資料已由其他操作更新，請重新載入後再試。"
        });
    }
    catch (InvalidOperationException ex)
    {
        return Conflict(new { Message = ex.Message });
    }
}
}
