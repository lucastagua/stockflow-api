namespace StockFlow.Api.DTOs;

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}