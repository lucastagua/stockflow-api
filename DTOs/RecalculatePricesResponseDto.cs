namespace StockFlow.Api.DTOs;

public class RecalculatePricesResponseDto
{
    public int UpdatedProducts { get; set; }

    public decimal ExchangeRate { get; set; }

    public DateTime ExchangeRateDate { get; set; }
}