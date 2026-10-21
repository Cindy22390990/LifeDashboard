using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace LifeDashboard.Api.Controllers
{
    // 系統設定 API 控制器
    // 負責接收前端的 HTTP 請求，並轉交給 Application 層的 Service 處理
    // 為什麼這樣設計：Controller 必須保持乾淨 (瘦 Controller)，絕對不可以在這裡寫「if(比例加總 != 100)」這種商業邏輯。它只負責路由、接資料、回傳 HTTP Status Code。
    // 如果要修改功能應該修改哪裡：如果前端需要新的 API 路徑（例如：重設密碼），請在這裡新增對應的 [HttpPost] 或 [HttpPut] 方法。
    [ApiController]
    [Route("api/[controller]")]
    // /api/Setting
    public class SettingController : ControllerBase
    {
        private readonly ISettingService _settingService;

        public SettingController(ISettingService settingService)
        {
            _settingService = settingService;
        }

        // GET: api/setting
        // 取得目前使用者的系統設定
        [HttpGet]
        public async Task<IActionResult> GetSetting()
        {
            try
            {
                // TODO: 未來接上 Infrastructure 的 JwtTokenService 後，UserId 應該從 User.Claims (Token) 中取得。
                // 這裡先暫時用一個假定的 Guid 示意，確保編譯與流程順暢。
                var currentUserId = Guid.Parse("00000000-0000-0000-0000-000000000000");

                var result = await _settingService.GetSettingAsync(currentUserId);
                return Ok(result); // 200 OK，回傳 SystemSettingDto
            }
            catch (Exception ex)
            {
                return NotFound(new { Message = ex.Message }); // 404
            }
        }
        // POST: api/setting
        // 初始化使用者的系統設定 (第一次使用時呼叫)
        [HttpPost]
        public async Task<IActionResult> InitializeSetting([FromBody] SystemSettingDto dto)
        {
            try
            {
                // 假定 UserId，未來從 JWT Token 拿
                var currentUserId = Guid.Parse("00000000-0000-0000-0000-000000000000");

                // 呼叫初始化服務
                await _settingService.InitializeSettingAsync(currentUserId, dto);

                return Created("", new { Message = "系統設定初始化成功！" }); // 201 Created
            }
            catch (InvalidOperationException ex)
            {
                // 已存在的防呆錯誤
                return Conflict(new { Message = ex.Message }); // 409 Conflict
            }
            catch (ArgumentException ex)
            {
                // 比例或發薪日驗證錯誤
                return BadRequest(new { Message = ex.Message }); // 400 Bad Request
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "伺服器發生未預期的錯誤", Detail = ex.Message });
            }
        }
        // PUT: api/setting
        // 更新目前使用者的系統設定 (發薪日、分配比例等)
        [HttpPut]
        public async Task<IActionResult> UpdateSetting([FromBody] SystemSettingDto dto)
        {
            try
            {
                // TODO: 同上，UserId 未來應從 Token 取得
                var currentUserId = Guid.Parse("00000000-0000-0000-0000-000000000000");

                // 將資料丟給 Service 處理。所有的商業邏輯驗證（例如 100% 檢查）都會在底層的 Domain 發生。
                await _settingService.UpdateSettingAsync(currentUserId, dto);

                return Ok(new { Message = "系統設定更新成功" }); // 200 OK
            }
            catch (ArgumentException ex)
            {
                // 攔截 Domain 層拋出的驗證錯誤 (例如：比例總和不是 100%)
                // 回傳 400 Bad Request 給前端，前端攔截後可以顯示對應的紅字提示
                return BadRequest(new { Message = ex.Message });
            }
            catch(KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch(Exception)
            {
                return StatusCode(500,new
                {
                    Message="伺服器錯誤"
                });
            }
        }
    }
}