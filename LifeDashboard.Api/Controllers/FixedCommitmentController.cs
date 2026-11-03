using LifeDashboard.Api.Extensions;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeDashboard.Api.Controllers;

[ApiController]
[Route("api/fixed-commitments")]
[Authorize]
public class FixedCommitmentController : ControllerBase
{
    private readonly IFixedCommitmentService _fixedCommitmentService;

    public FixedCommitmentController(IFixedCommitmentService fixedCommitmentService)
    {
        _fixedCommitmentService = fixedCommitmentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FixedCommitmentDto>>> GetFixedCommitments()
    {
        var userId = User.GetUserId();
        return Ok(await _fixedCommitmentService.GetAllAsync(userId));
    }

    [HttpPost]
    public async Task<ActionResult<FixedCommitmentDto>> CreateFixedCommitment(
        CreateFixedCommitmentDto dto)
    {
        var userId = User.GetUserId();
        try
        {
            var commitment = await _fixedCommitmentService.CreateAsync(userId, dto);
            return StatusCode(StatusCodes.Status201Created, commitment);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateFixedCommitment(
        Guid id,
        UpdateFixedCommitmentDto dto)
    {
        try
        {
            var userId = User.GetUserId();
            await _fixedCommitmentService.UpdateAsync(id, userId, dto);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteFixedCommitment(Guid id)
    {
        try
        {
            var userId = User.GetUserId();
            await _fixedCommitmentService.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }
}
