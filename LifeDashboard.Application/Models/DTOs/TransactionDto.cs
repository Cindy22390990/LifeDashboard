namespace LifeDashboard.Application.Models.DTOs
{
    public class TransactionDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public decimal Amount { get; set; }
        public int Type { get; set; }
        public string? CategoryId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateOnly TransactionDate { get; set; }
    }
}