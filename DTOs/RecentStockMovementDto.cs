using StockFlow.Api.Models;

namespace StockFlow.Api.DTOs;

public class RecentStockMovementDto
{
    public int Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public StockMovementType Type { get; set; }

    public int Quantity { get; set; }

    public int PreviousStock { get; set; }

    public int NewStock { get; set; }

    public DateTime CreatedAt { get; set; }
}