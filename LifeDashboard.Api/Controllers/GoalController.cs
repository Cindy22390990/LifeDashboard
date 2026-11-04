using LifeDashboard.Api.Extensions;
using LifeDashboard.Application.Exceptions;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/goals")]
    [Authorize]
    public class GoalController : ControllerBase
    {
        private readonly IGoalService _goalService;
        private readonly IGoalDepositService _goalDepositService;
        private readonly IGoalPurchaseService _goalPurchaseService;

        public GoalController(
            IGoalService goalService,
            IGoalDepositService goalDepositService,
            IGoalPurchaseService goalPurchaseService)
        {
            _goalService = goalService;
            _goalDepositService = goalDepositService;
            _goalPurchaseService = goalPurchaseService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<GoalDto>>> GetGoals(
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            return Ok(await _goalService.GetAllAsync(userId, cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<GoalDto>> GetGoal(
            int id,
            CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var goal = await _goalService.GetByIdAsync(id, userId, cancellationToken);
            return goal is null ? NotFound() : Ok(goal);
        }

        [HttpPost]
        public async Task<ActionResult<GoalDto>> CreateGoal(
            CreateGoalDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var goal = await _goalService.CreateAsync(
                    User.GetUserId(),
                    dto,
                    cancellationToken);
                return CreatedAtAction(nameof(GetGoal), new { id = goal.Id }, goal);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateGoal(
            int id,
            UpdateGoalDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                await _goalService.UpdateAsync(
                    id,
                    User.GetUserId(),
                    dto,
                    cancellationToken);
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

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteGoal(
            int id,
            CancellationToken cancellationToken)
        {
            try
            {
                await _goalService.DeleteAsync(
                    id,
                    User.GetUserId(),
                    cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(new { message = exception.Message });
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }

        [HttpPost("{id:int}/deposit")]
        public async Task<ActionResult<GoalDto>> Deposit(
            int id,
            GoalDepositRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var goal = await _goalDepositService.DepositAsync(
                    id,
                    User.GetUserId(),
                    request,
                    cancellationToken);
                return Ok(goal);
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(new { message = exception.Message });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new
                {
                    message = "帳戶資料已由其他操作更新，請重新載入後再試。"
                });
            }
            catch (InsufficientFundsException exception)
            {
                return Conflict(new { message = exception.Message });
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }

        [HttpPost("{id:int}/purchase")]
        public async Task<ActionResult<GoalDto>> Purchase(
            int id,
            GoalPurchaseRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var goal = await _goalPurchaseService.PurchaseAsync(
                    id,
                    User.GetUserId(),
                    request,
                    cancellationToken);
                return Ok(goal);
            }
            catch (KeyNotFoundException exception)
            {
                return NotFound(new { message = exception.Message });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new
                {
                    message = "帳戶資料已由其他操作更新，請重新載入後再試。"
                });
            }
            catch (InsufficientFundsException exception)
            {
                return Conflict(new { message = exception.Message });
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { message = exception.Message });
            }
        }
    }
}
