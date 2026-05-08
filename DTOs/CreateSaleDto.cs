namespace StockFlow.Api.DTOs;

public class CreateSaleDto
{
    public List<CreateSaleItemDto> Items { get; set; } = new();
}