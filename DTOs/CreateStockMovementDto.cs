using StockFlow.Api.Models;

namespace StockFlow.Api.DTOs;

public class CreateStockMovementDto
{
    public int ProductId { get; set; }

    public StockMovementType Type { get; set; }

    public int Quantity { get; set; }

    public string? Reason { get; set; }
}