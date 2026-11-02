using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace LifeDashboard.Api.Controllers;


[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{


    // =====================================
    // 測試 JWT 是否成功
    //
    // 必須:
    // Header:
    // Authorization: Bearer Token
    //
    // =====================================

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {


        var userId =
            User.FindFirst(
                ClaimTypes.NameIdentifier)
            ?.Value;


        var username =
            User.FindFirst(
                ClaimTypes.Name)
            ?.Value;



        return Ok(new
        {
            UserId = userId,

            Username = username
        });

    }

}