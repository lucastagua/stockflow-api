namespace StockFlow.Api.Models;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public string? Sku { get; set; }

    public decimal CostUsd { get; set; }

    public decimal ProfitMarginPercentage { get; set; }

    public decimal PriceArs { get; set; }

    public int Stock { get; set; }

    public int MinimumStock { get; set; } = 3;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int CategoryId { get; set; }

    public Category? Category { get; set; }

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}