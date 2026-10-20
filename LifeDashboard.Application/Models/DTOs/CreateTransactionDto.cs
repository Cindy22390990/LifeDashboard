namespace LifeDashboard.Application.Models.DTOs
{
    public class CreateTransactionDto
    {
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public int Type { get; set; } // 1: 支出, 2: 收入, 3: 轉帳
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
    }
}