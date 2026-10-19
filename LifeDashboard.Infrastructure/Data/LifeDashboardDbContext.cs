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
            // ====== 解決 Decimal 精確度警告 ======
            // 為什麼這樣設計：記帳 APP 的金額絕對不能被隨便捨去小數點。
            // HasPrecision(18, 2) 代表：總共最多 18 位數，其中小數點保留 2 位。
            modelBuilder.Entity<Account>().Property(a => a.Balance).HasPrecision(18, 2);
            modelBuilder.Entity<Goal>().Property(g => g.TargetAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Goal>().Property(g => g.CurrentAmount).HasPrecision(18, 2);
            modelBuilder.Entity<Transaction>().Property(t => t.Amount).HasPrecision(18, 2);
            // ==========================================
            // 1. Transaction (交易) 的資料庫映射設定
            // ==========================================
            modelBuilder.Entity<Transaction>(entity =>
                {
                    // 配置主要帳戶 (支出、收入、轉出的來源)
                    entity.HasOne(t => t.Account)
                          .WithMany(a => a.Transactions)
                          .HasForeignKey(t => t.AccountId)
                          .OnDelete(DeleteBehavior.Restrict);

                    // 配置轉帳目的帳戶 (轉入的目的地)
                    entity.HasOne(t => t.ToAccount)
                          .WithMany()
                          .HasForeignKey(t => t.ToAccountId)
                          .OnDelete(DeleteBehavior.Restrict);
                });
                // ==========================================
                // 2. SystemSetting (系統設定) 的資料庫映射設定
                // ==========================================
                modelBuilder.Entity<SystemSetting>(entity =>
                {
                    // 設定 Table 名稱
                    entity.ToTable("UserSettings");

                    // 設定主鍵
                    entity.HasKey(e => e.Id);

                    // 設定 UserName 最大長度，節省資料庫空間
                    entity.Property(e => e.UserName).HasMaxLength(50);

                    // 必須設定 PasswordHash 為必填
                    entity.Property(e => e.PasswordHash).IsRequired();

                    // DDD Value Object 核心設定
                    entity.OwnsOne(e => e.Allocation, a =>
                    {
                        a.Property(p => p.Living).HasColumnName("AllocLiving");
                        a.Property(p => p.Savings).HasColumnName("AllocSavings");
                        a.Property(p => p.Investment).HasColumnName("AllocInvestment");
                        a.Property(p => p.Entertainment).HasColumnName("AllocEntertainment");
                    });
                });
        }
    }
}