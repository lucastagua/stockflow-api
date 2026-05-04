namespace StockFlow.Api.DTOs;

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;

    public string? Brand { get; set; }

    public string? Sku { get; set; }

    public decimal CostUsd { get; set; }

    public decimal PriceArs { get; set; }

    public int Stock { get; set; }

    public int MinimumStock { get; set; } = 3;

    public int CategoryId { get; set; }
}