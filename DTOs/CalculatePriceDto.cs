namespace StockFlow.Api.DTOs;

public class CalculatePriceDto
{
    public decimal CostUsd { get; set; }

    public decimal ProfitMarginPercentage { get; set; }
}