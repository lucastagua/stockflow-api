using StockFlow.Api.DTOs;
using StockFlow.Api.Models;

namespace StockFlow.Api.Mappings;

public static class ProductMapper
{
    public static ProductResponseDto ToResponseDto(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Brand = product.Brand,
            Sku = product.Sku,
            CostUsd = product.CostUsd,
            ProfitMarginPercentage = product.ProfitMarginPercentage,
            PriceArs = product.PriceArs,
            Stock = product.Stock,
            MinimumStock = product.MinimumStock,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            CategoryId = product.CategoryId,
            CategoryName = product.Category != null ? product.Category.Name : string.Empty
        };
    }
}