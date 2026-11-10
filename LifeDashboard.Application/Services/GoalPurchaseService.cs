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
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITransactionService _transactionService;
    private readonly IUnitOfWork _unitOfWork;

    public GoalPurchaseService(
        IGoalRepository goalRepository,
        ICategoryRepository categoryRepository,
        ITransactionService transactionService,
        IUnitOfWork unitOfWork)
    {
        _goalRepository = goalRepository;
        _categoryRepository = categoryRepository;
        _transactionService = transactionService;
        _unitOfWork = unitOfWork;
    }

    public async Task<GoalDto> PurchaseAsync(
        int goalId,
        Guid userId,
        GoalPurchaseRequest request,
        CancellationToken cancellationToken)
    {
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

        if (request.ActualAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.ActualAmount),
                "購買金額必須大於 0。");
        }

        var category = await _categoryRepository.GetByIdAsync(
            request.CategoryId,
            userId)
            ?? throw new KeyNotFoundException("找不到指定的分類。");

        if (category.BucketType == BucketType.Living)
        {
            throw new ArgumentException("購買目標不可使用生活費分類。", nameof(request.CategoryId));
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _transactionService.CreateTransactionWithinExistingTransactionAsync(
                userId,
                new CreateTransactionDto
                {
                    AccountId = request.AccountId,
                    CategoryId = request.CategoryId,
                    Amount = request.ActualAmount,
                    Type = (int)TransactionType.Expense,
                    Description = request.Note,
                    TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow)
                },
                goal);

            goal.CompletePurchase();

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
