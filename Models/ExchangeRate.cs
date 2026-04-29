namespace StockFlow.Api.Models;

public class ExchangeRate
{
    public int Id { get; set; }

    public decimal Value { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;
}