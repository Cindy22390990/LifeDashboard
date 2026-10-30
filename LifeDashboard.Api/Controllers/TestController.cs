using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace LifeDashboard.Api.Controllers;


// ============================================
// JWT 測試 Controller
//
// 目的:
// 確認登入取得 Token 後
// 是否可以通過身份驗證
// ============================================

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    // GET api/test/public
    // 不需要登入
    [HttpGet("public")]
    public IActionResult Public()
    {
        return Ok(new
        {
            message = "公開 API"
        });
    }



    // GET api/test/private
    // 需要 JWT
    [Authorize]
    [HttpGet("private")]
    public IActionResult Private()
    {

        return Ok(new
        {
            message = "JWT 驗證成功",

            user =
                User.Identity?.Name
        });
    }

}