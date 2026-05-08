namespace StockFlow.Api.DTOs;

public class SaleItemResponseDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPriceArs { get; set; }

    public decimal SubtotalArs { get; set; }
}