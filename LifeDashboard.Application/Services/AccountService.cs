using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Domain.Entities;

namespace LifeDashboard.Application.Services
{
    // 帳戶應用服務實作
    // 負責處理商業邏輯，並透過 Repository 操作資料，完全不依賴 EF Core
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepo;

        public AccountService(IAccountRepository accountRepo)
        {
            _accountRepo = accountRepo; // 依賴注入 Repository
        }

        public async Task<IEnumerable<AccountDto>> GetAllAccountsAsync(
            Guid userId)
        {
            var accounts =
                await _accountRepo.GetAllAsync(userId);


            return accounts.Select(a => new AccountDto
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type,
                CurrentBalance = a.InitialBalance,
                IsSystemAccount = a.IsSystemAccount
            });
        }

        public async Task<AccountDto> CreateAccountAsync(
            Guid userId,
            CreateAccountDto dto)
        {
            Console.WriteLine("==============");
            Console.WriteLine(userId);
            Console.WriteLine("==============");
            
            var account =
                new Account(
                    userId,
                    dto.Name,
                    dto.Type,
                    dto.InitialBalance);
            
            Console.WriteLine($"UserId = {account.UserId}");
            await _accountRepo.AddAsync(account);

            await _accountRepo.SaveChangesAsync();


            return new AccountDto
            {
                Id = account.Id,
                Name = account.Name,
                Type = account.Type,
                CurrentBalance = account.InitialBalance,
                IsSystemAccount = account.IsSystemAccount
            };
        }

        public async Task UpdateAccountAsync(
            Guid id,
            Guid userId,
            UpdateAccountDto dto)
        {
            var account =
                await _accountRepo.GetByIdAsync(
                    id,
                    userId);


            if (account == null)
            {
                throw new KeyNotFoundException(
                    "找不到指定的帳戶。");
            }


            account.UpdateDetails(
                dto.Name,
                dto.Type);


            await _accountRepo.SaveChangesAsync();
        }

        public async Task SoftDeleteAccountAsync(
            Guid id,
            Guid userId)
        {
            var account =
                await _accountRepo.GetByIdAsync(
                    id,
                    userId);


            if (account == null)
            {
                throw new KeyNotFoundException(
                    "找不到指定的帳戶。");
            }


            account.MarkAsDeleted();


            await _accountRepo.SaveChangesAsync();
        }
    }
}