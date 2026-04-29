namespace StockFlow.Api.DTOs;

public class UpdateCategoryDto
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}