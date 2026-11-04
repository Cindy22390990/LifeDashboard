using LifeDashboard.Application.Services;
using LifeDashboard.Application.Interfaces;
using LifeDashboard.Domain.Entities;
using LifeDashboard.Domain.Enums;
using LifeDashboard.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransactionEntity = LifeDashboard.Domain.Entities.Transaction;

namespace LifeDashboard.Tests.Transaction;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("LIFEDASHBOARD_SQLSERVER_TEST_CONNECTION")))
        {
            Skip = "Set LIFEDASHBOARD_SQLSERVER_TEST_CONNECTION to run SQL Server concurrency tests.";
        }
    }
}

public class SqlServerAccountConcurrencyTests
{
    [SqlServerFact]
    public async Task Concurrent_debits_do_not_overdraw_or_persist_failed_transaction()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable(
            "LIFEDASHBOARD_SQLSERVER_TEST_CONNECTION")!;
        var connectionStringBuilder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = $"LifeDashboardConcurrency_{Guid.NewGuid():N}"
        };
        var options = new DbContextOptionsBuilder<LifeDashboardDbContext>()
            .UseSqlServer(connectionStringBuilder.ConnectionString)
            .Options;
        var user = new User($"ConcurrencyUser-{Guid.NewGuid():N}", "test-hash");
        var account = new Account(
            user.Id,
            "Concurrent account",
            AccountType.Cashflow,
            100m)
        {
            CurrentBalance = 100m
        };
        var databaseCreated = false;

        try
        {
            await using (var setupContext = new LifeDashboardDbContext(options))
            {
                await setupContext.Database.EnsureCreatedAsync();
                databaseCreated = true;
                setupContext.Users.Add(user);
                setupContext.Accounts.Add(account);
                await setupContext.SaveChangesAsync();
            }

            var bothRequestsLoaded = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var loadedRequestCount = 0;

            async Task<DbUpdateConcurrencyException?> AttemptDebitAsync()
            {
                await using var dbContext = new LifeDashboardDbContext(options);
                var unitOfWork = new UnitOfWork(dbContext);
                await unitOfWork.BeginTransactionAsync();
                try
                {
                    var trackedAccount = await dbContext.Accounts
                        .SingleAsync(entity => entity.Id == account.Id);
                    new AccountDebitService().Debit(trackedAccount, 75m);
                    dbContext.Transactions.Add(new TransactionEntity(
                        user.Id,
                        account.Id,
                        75m,
                        TransactionType.Expense,
                        "Concurrent debit",
                        new DateOnly(2026, 11, 4)));

                    if (Interlocked.Increment(ref loadedRequestCount) == 2)
                    {
                        bothRequestsLoaded.SetResult();
                    }

                    await bothRequestsLoaded.Task;
                    await unitOfWork.SaveChangesAsync();
                    await unitOfWork.CommitAsync();
                    return null;
                }
                catch (DbUpdateConcurrencyException exception)
                {
                    await unitOfWork.RollbackAsync();
                    return exception;
                }
                catch
                {
                    await unitOfWork.RollbackAsync();
                    throw;
                }
            }

            var outcomes = await Task.WhenAll(
                AttemptDebitAsync(),
                AttemptDebitAsync());

            Assert.Single(outcomes, outcome => outcome is null);
            Assert.Single(outcomes, outcome => outcome is not null);
            Assert.All(
                outcomes.Where(outcome => outcome is not null),
                outcome => Assert.IsType<DbUpdateConcurrencyException>(outcome));

            await using var verificationContext = new LifeDashboardDbContext(options);
            var persistedAccount = await verificationContext.Accounts
                .AsNoTracking()
                .SingleAsync(entity => entity.Id == account.Id);
            Assert.Equal(25m, persistedAccount.CurrentBalance);
            Assert.Single(await verificationContext.Transactions
                .AsNoTracking()
                .ToListAsync());
        }
        finally
        {
            if (databaseCreated)
            {
                await using var cleanupContext = new LifeDashboardDbContext(options);
                await cleanupContext.Database.EnsureDeletedAsync();
            }
        }
    }

    [SqlServerFact]
    public async Task Concurrent_policy_update_and_debit_are_serialized_by_rowversion()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable(
            "LIFEDASHBOARD_SQLSERVER_TEST_CONNECTION")!;
        var connectionStringBuilder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = $"LifeDashboardPolicyRace_{Guid.NewGuid():N}"
        };
        var options = new DbContextOptionsBuilder<LifeDashboardDbContext>()
            .UseSqlServer(connectionStringBuilder.ConnectionString)
            .Options;
        var user = new User($"ConcurrencyUser-{Guid.NewGuid():N}", "test-hash");
        var account = new Account(
            user.Id,
            "Policy race account",
            AccountType.Cashflow,
            100m)
        {
            CurrentBalance = 100m
        };
        var databaseCreated = false;

        try
        {
            await using (var setupContext = new LifeDashboardDbContext(options))
            {
                await setupContext.Database.EnsureCreatedAsync();
                databaseCreated = true;
                setupContext.Users.Add(user);
                setupContext.Accounts.Add(account);
                await setupContext.SaveChangesAsync();
            }

            var bothRequestsLoaded = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var loadedRequestCount = 0;

            async Task<bool> AttemptDebitAsync()
            {
                await using var dbContext = new LifeDashboardDbContext(options);
                var unitOfWork = new UnitOfWork(dbContext);
                await unitOfWork.BeginTransactionAsync();
                try
                {
                    var trackedAccount = await dbContext.Accounts
                        .SingleAsync(entity => entity.Id == account.Id);
                    new AccountDebitService().Debit(trackedAccount, 75m);
                    dbContext.Transactions.Add(new TransactionEntity(
                        user.Id,
                        account.Id,
                        75m,
                        TransactionType.Expense,
                        "Policy race debit",
                        new DateOnly(2026, 11, 4)));
                    SignalBothLoaded();

                    await bothRequestsLoaded.Task;
                    await unitOfWork.SaveChangesAsync();
                    await unitOfWork.CommitAsync();
                    return true;
                }
                catch (DbUpdateConcurrencyException)
                {
                    await unitOfWork.RollbackAsync();
                    return false;
                }
                catch
                {
                    await unitOfWork.RollbackAsync();
                    throw;
                }
            }

            async Task<bool> AttemptPolicyUpdateAsync()
            {
                await using var dbContext = new LifeDashboardDbContext(options);
                var trackedAccount = await dbContext.Accounts
                    .SingleAsync(entity => entity.Id == account.Id);
                trackedAccount.AllowNegativeBalance = true;
                SignalBothLoaded();

                await bothRequestsLoaded.Task;
                try
                {
                    await dbContext.SaveChangesAsync();
                    return true;
                }
                catch (DbUpdateConcurrencyException)
                {
                    return false;
                }
            }

            void SignalBothLoaded()
            {
                if (Interlocked.Increment(ref loadedRequestCount) == 2)
                {
                    bothRequestsLoaded.SetResult();
                }
            }

            var outcomes = await Task.WhenAll(
                AttemptDebitAsync(),
                AttemptPolicyUpdateAsync());
            Assert.Single(outcomes, succeeded => succeeded);
            Assert.Single(outcomes, succeeded => !succeeded);

            await using var verificationContext = new LifeDashboardDbContext(options);
            var persistedAccount = await verificationContext.Accounts
                .AsNoTracking()
                .SingleAsync(entity => entity.Id == account.Id);
            var persistedTransactions = await verificationContext.Transactions
                .AsNoTracking()
                .ToListAsync();

            if (persistedAccount.AllowNegativeBalance)
            {
                Assert.Equal(100m, persistedAccount.CurrentBalance);
                Assert.Empty(persistedTransactions);
            }
            else
            {
                Assert.Equal(25m, persistedAccount.CurrentBalance);
                Assert.Single(persistedTransactions);
            }
        }
        finally
        {
            if (databaseCreated)
            {
                await using var cleanupContext = new LifeDashboardDbContext(options);
                await cleanupContext.Database.EnsureDeletedAsync();
            }
        }
    }
}
