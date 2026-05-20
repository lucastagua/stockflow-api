namespace StockFlow.Api.Helpers;

public static class PriceCalculator
{
    public static decimal CalculatePriceArs(
        decimal costUsd,
        decimal exchangeRate,
        decimal profitMarginPercentage)
    {
        var marginMultiplier = 1 + (profitMarginPercentage / 100);

        var calculatedPrice = costUsd * exchangeRate * marginMultiplier;

        return Math.Round(calculatedPrice, 2);
    }
}