namespace StockFlow.Api.Models;

public class Sale
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public decimal TotalAmountArs { get; set; }

    public SaleStatus Status { get; set; } = SaleStatus.Completed;

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}