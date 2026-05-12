using StockFlow.Api.Models;

namespace StockFlow.Api.DTOs;

public class RecentSaleDto
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal TotalAmountArs { get; set; }

    public SaleStatus Status { get; set; }
}