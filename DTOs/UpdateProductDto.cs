namespace StockFlow.Api.DTOs;

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public string? Sku { get; set; }

    public decimal CostUsd { get; set; }

    public decimal ProfitMarginPercentage { get; set; }

    public int Stock { get; set; }

    public int MinimumStock { get; set; } = 3;

    public bool IsActive { get; set; } = true;

    public int CategoryId { get; set; }
}