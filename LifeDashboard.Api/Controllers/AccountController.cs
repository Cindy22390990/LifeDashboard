using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using LifeDashboard.Api.Extensions;

namespace LifeDashboard.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase // 檔名與類別名稱保持一致
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        // GET: api/Account
        [Authorize]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccountDto>>> GetAccounts()
        {
            var userId =
                User.GetUserId();


            return Ok(
                await _accountService
                .GetAllAccountsAsync(userId));
        }
        
        // POST: api/Account
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<AccountDto>> CreateAccount(
            CreateAccountDto dto)
        {
            var userId = User.GetUserId();


            var result =
                await _accountService
                .CreateAccountAsync(
                    userId,
                    dto);


            return Ok(result);
        }

        // PUT: api/Account/{id}
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(
            Guid id,
            UpdateAccountDto dto)
        {
            try
            {
                var userId =
                    User.GetUserId();


                await _accountService
                    .UpdateAccountAsync(
                        id,
                        userId,
                        dto);


                return NoContent();
            }
            catch(KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }

        // DELETE: api/Account/{id}
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(
            Guid id)
        {
            try
            {
                var userId =
                    User.GetUserId();


                await _accountService
                    .SoftDeleteAccountAsync(
                        id,
                        userId);


                return NoContent();
            }
            catch(KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch(InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}