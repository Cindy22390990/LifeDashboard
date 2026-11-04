using LifeDashboard.Application.Interfaces;
using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;

namespace LifeDashboard.Application.Services;

public sealed class GoalPurchaseService : IGoalPurchaseService
{
    private readonly IGoalRepository _goalRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountDebitService _accountDebitService;
    private readonly IUnitOfWork _unitOfWork;

    public GoalPurchaseService(
        IGoalRepository goalRepository,
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository,
        ITransactionRepository transactionRepository,
        IAccountDebitService accountDebitService,
        IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _transactionRepository = transactionRepository;
        _accountDebitService = accountDebitService;
        _unitOfWork = unitOfWork;
    }

    public async Task<GoalDto> PurchaseAsync(
        int goalId,
        Guid userId,
        GoalPurchaseRequest request,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (request.ActualAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request.ActualAmount),
                    "購買金額必須大於 0。");
            }

            var goal = await _goalRepository.GetByIdAsync(
                goalId,
                userId,
                cancellationToken)
                ?? throw new KeyNotFoundException("找不到指定的目標。");

            if (goal.GoalType != GoalType.Purchase)
            {
                throw new InvalidOperationException("只有購物願望目標可以完成購買。");
            }

            if (goal.Status != GoalStatus.Active)
            {
                throw new InvalidOperationException("目標已完成，不可再次購買。");
            }

            if (goal.CurrentAmount < goal.TargetAmount)
            {
                throw new InvalidOperationException("目標尚未達標，不可完成購買。");
            }

            var account = await _accountRepository.GetByIdAsync(
                request.AccountId,
                userId)
                ?? throw new KeyNotFoundException("找不到指定的付款帳戶。");

            var category = await _categoryRepository.GetByIdAsync(
                request.CategoryId,
                userId)
                ?? throw new KeyNotFoundException("找不到指定的分類。");

            if (category.BucketType == BucketType.Living)
            {
                throw new ArgumentException("購買目標不可使用生活費分類。", nameof(request.CategoryId));
            }

            var transaction = new Transaction(
                userId,
                account.Id,
                request.ActualAmount,
                TransactionType.Expense,
                request.Note,
                DateOnly.FromDateTime(DateTime.UtcNow));
            transaction.CategoryId = category.Id;
            transaction.CategoryEntity = category;
            transaction.AssociateWithGoal(goal);

            _accountDebitService.Debit(account, request.ActualAmount);
            goal.CompletePurchase();
            await _transactionRepository.AddAsync(transaction);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            return ToDto(goal);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private static GoalDto ToDto(Goal goal)
    {
        return new GoalDto
        {
            Id = goal.Id,
            Name = goal.Name,
            TargetAmount = goal.TargetAmount,
            CurrentAmount = goal.CurrentAmount,
            Tier = goal.Tier,
            GoalType = goal.GoalType,
            Status = goal.Status,
            Note = goal.Note,
            CompletedDate = goal.CompletedDate
        };
    }
}
