namespace StockFlow.Api.DTOs;

public class ProductResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public string? Sku { get; set; }

    public decimal CostUsd { get; set; }

    public decimal ProfitMarginPercentage { get; set; }

    public decimal PriceArs { get; set; }

    public int Stock { get; set; }

    public int MinimumStock { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;
}