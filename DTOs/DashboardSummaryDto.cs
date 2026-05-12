namespace StockFlow.Api.DTOs;

public class DashboardSummaryDto
{
    public int TotalProducts { get; set; }

    public int ActiveProducts { get; set; }

    public int LowStockProducts { get; set; }

    public int CompletedSales { get; set; }

    public int CancelledSales { get; set; }

    public decimal TotalRevenueArs { get; set; }

    public List<RecentSaleDto> RecentSales { get; set; } = new();

    public List<RecentStockMovementDto> RecentStockMovements { get; set; } = new();
}