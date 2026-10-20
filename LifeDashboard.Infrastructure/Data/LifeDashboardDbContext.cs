using LifeDashboard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LifeDashboard.Infrastructure.Data
{
    // 資料庫上下文 (DbContext)
    // 負責定義程式碼裡的 Entities 要如何對應到資料庫的 Tables
    public class LifeDashboardDbContext : DbContext
    {
        public LifeDashboardDbContext(DbContextOptions<LifeDashboardDbContext> options)
            : base(options)
        {
        }

        // 現有的 DbSets (原本應該就有 Account, Transaction 等)
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Goal> Goals { get; set; }

        // ====== 這是我們新增的 ======
        // 系統設定資料表
        public DbSet<SystemSetting> SystemSettings { get; set; }

        // 設定 Entity 欄位與資料庫 Table 欄位的對應關係
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.InitialBalance)
                    .HasPrecision(18,2);

                entity.Property(e => e.CurrentBalance)
                    .HasPrecision(18,2);

                entity.HasQueryFilter(a => !a.IsDeleted);
            });


            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.Property(t => t.Amount)
                    .HasPrecision(18,2);

                entity.HasOne(t => t.Account)
                    .WithMany(a => a.Transactions)
                    .HasForeignKey(t => t.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.ToAccount)
                    .WithMany()
                    .HasForeignKey(t => t.ToAccountId)
                    .OnDelete(DeleteBehavior.Restrict);
            });


            modelBuilder.Entity<Goal>()
                .Property(g => g.TargetAmount)
                .HasPrecision(18,2);

            modelBuilder.Entity<Goal>()
                .Property(g => g.CurrentAmount)
                .HasPrecision(18,2);


            modelBuilder.Entity<SystemSetting>(entity =>
            {
                entity.OwnsOne(
                    e => e.Allocation,
                    allocation =>
                    {
                        allocation.Property(p => p.Living)
                            .HasColumnName("AllocLiving");

                        allocation.Property(p => p.Savings)
                            .HasColumnName("AllocSavings");

                        allocation.Property(p => p.Investment)
                            .HasColumnName("AllocInvestment");

                        allocation.Property(p => p.Entertainment)
                            .HasColumnName("AllocEntertainment");
                    });
            });
        }
    }
}