using LifeDashboard.Application.Models.DTOs;
using LifeDashboard.Application.Repositories.Interfaces;
using LifeDashboard.Application.Services;
using LifeDashboard.Application.Services.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using Moq;

namespace LifeDashboard.Tests.Helpers;

public static class TestDataFactory
{
    public static SystemSettingDto CreateSetting(int payday)
    {
        return new SystemSettingDto
        {
            Payday = payday
        };
    }

    public static FixedCommitment CreateFixedCommitment(
        decimal monthlyAmount,
        DateOnly startMonth,
        DateOnly? endMonth = null,
        Guid? userId = null)
    {
        return new FixedCommitment(
            userId ?? Guid.NewGuid(),
            "Test commitment",
            monthlyAmount,
            CommitmentType.Installment,
            startMonth,
            endMonth);
    }

    public static BudgetRadarTestContext CreateBudgetRadarService(int payday)
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        transactionRepository
            .Setup(repository => repository.GetTotalAmountAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<TransactionType>(),
                It.IsAny<BucketType?>()))
            .ReturnsAsync(0m);
        transactionRepository
            .Setup(repository => repository.GetUncategorizedExpenseTotalAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>()))
            .ReturnsAsync(0m);

        var fixedCommitmentRepository = new Mock<IFixedCommitmentRepository>();
        fixedCommitmentRepository
            .Setup(repository => repository.GetActiveForPayrollPeriodAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>()))
            .ReturnsAsync(Array.Empty<FixedCommitment>());

        var settingService = new Mock<ISettingService>();
        settingService
            .Setup(service => service.GetSettingAsync(It.IsAny<Guid>()))
            .ReturnsAsync(CreateSetting(payday));

        var service = new BudgetRadarService(
            transactionRepository.Object,
            fixedCommitmentRepository.Object,
            settingService.Object);

        return new BudgetRadarTestContext(
            service,
            transactionRepository,
            fixedCommitmentRepository,
            settingService);
    }

    public sealed record BudgetRadarTestContext(
        BudgetRadarService Service,
        Mock<ITransactionRepository> TransactionRepository,
        Mock<IFixedCommitmentRepository> FixedCommitmentRepository,
        Mock<ISettingService> SettingService);
}
