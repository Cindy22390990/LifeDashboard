using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Models.DTOs
{
    // 前端獲取帳戶列表時的格式
    public class AccountDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        
        // 未來將包含關聯交易計算，目前第一階段先回傳 InitialBalance
        public decimal CurrentBalance { get; set; }
        
        // 供前端判斷是否隱藏「刪除按鈕」
        public bool IsSystemAccount { get; set; }
    }

    // 前端發送「新增帳戶」請求時的格式
    public class CreateAccountDto
    {
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; }
        public decimal InitialBalance { get; set; }
    }

    // 前端發送「修改帳戶」請求時的格式
    public class UpdateAccountDto
    {
        public string Name { get; set; } = string.Empty;
        public AccountType Type { get; set; }
    }
}