using LifeDashboard.Api.Extensions;
using LifeDashboard.Application.Features.V4.Dtos;
using LifeDashboard.Application.Features.V4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeDashboard.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public sealed class V4AccountsController : ControllerBase
{
    private readonly IV4AccountsService _accountsService;

    public V4AccountsController(IV4AccountsService accountsService)
    {
        _accountsService = accountsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AccountsReadDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AccountsReadDto>> GetAccounts(
        CancellationToken cancellationToken)
    {
        var result = await _accountsService.GetAccountsAsync(
            User.GetUserId(),
            cancellationToken);

        return Ok(result);
    }
}
