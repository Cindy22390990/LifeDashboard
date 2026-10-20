using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AccountDto>>> GetAccounts()
        {
            return Ok(await _accountService.GetAllAccountsAsync());
        }

        // POST: api/Account
        [HttpPost]
        public async Task<ActionResult<AccountDto>> CreateAccount(CreateAccountDto dto)
        {
            var result = await _accountService.CreateAccountAsync(dto);
            return CreatedAtAction(nameof(GetAccounts), new { id = result.Id }, result);
        }

        // PUT: api/Account/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(Guid id, UpdateAccountDto dto)
        {
            try
            {
                await _accountService.UpdateAccountAsync(id, dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        // DELETE: api/Account/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(Guid id)
        {
            try
            {
                await _accountService.SoftDeleteAccountAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}