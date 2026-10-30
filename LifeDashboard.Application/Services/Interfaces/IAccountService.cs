using LifeDashboard.Application.Models.DTOs;

namespace LifeDashboard.Application.Services.Interfaces;

    // 帳戶應用服務介面
    // 定義前端可以對帳戶執行的所有操作
    public interface IAccountService
    {
        // 取得帳戶列表
        Task<IEnumerable<AccountDto>>
            GetAllAccountsAsync(
                Guid userId);

        // 新增帳戶
        Task<AccountDto>
            CreateAccountAsync(
                Guid userId,
                CreateAccountDto dto);

        // 修改帳戶 (不回傳值，使用 HTTP 204 No Content)
       Task UpdateAccountAsync(
            Guid id,
            Guid userId,
            UpdateAccountDto dto);

        // 軟刪除帳戶
         Task SoftDeleteAccountAsync(
            Guid id,
            Guid userId);
    }
