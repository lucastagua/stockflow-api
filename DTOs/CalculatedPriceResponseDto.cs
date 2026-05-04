namespace StockFlow.Api.DTOs;

public class CalculatedPriceResponseDto
{
    public decimal CostUsd { get; set; }

    public decimal ExchangeRate { get; set; }

    public decimal ProfitMarginPercentage { get; set; }

    public decimal SuggestedPriceArs { get; set; }
}