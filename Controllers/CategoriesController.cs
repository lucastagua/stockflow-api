using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;
using StockFlow.Api.Models;
using StockFlow.Api.DTOs;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Category>> GetCategoryById(int id)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<Category>> CreateCategory(CreateCategoryDto createCategoryDto)
    {
        if (string.IsNullOrWhiteSpace(createCategoryDto.Name))
        {
            return BadRequest(new
            {
                message = "Category name is required."
            });
        }

        var exists = await _context.Categories
            .AnyAsync(c => c.Name.ToLower() == createCategoryDto.Name.ToLower());

        if (exists)
        {
            return Conflict(new
            {
                message = "A category with this name already exists."
            });
        }

        var category = new Category
        {
            Name = createCategoryDto.Name.Trim(),
            IsActive = createCategoryDto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCategoryById),
            new { id = category.Id },
            category
        );
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, UpdateCategoryDto updateCategoryDto)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        if (string.IsNullOrWhiteSpace(updateCategoryDto.Name))
        {
            return BadRequest(new
            {
                message = "Category name is required."
            });
        }

        var nameAlreadyExists = await _context.Categories
            .AnyAsync(c =>
                c.Id != id &&
                c.Name.ToLower() == updateCategoryDto.Name.ToLower()
            );

        if (nameAlreadyExists)
        {
            return Conflict(new
            {
                message = "Another category with this name already exists."
            });
        }

        category.Name = updateCategoryDto.Name.Trim();
        category.IsActive = updateCategoryDto.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        if (!category.IsActive)
        {
            return BadRequest(new
            {
                message = "Category is already inactive."
            });
        }

        var hasActiveProducts = await _context.Products
            .AnyAsync(p => p.CategoryId == id && p.IsActive);

        if (hasActiveProducts)
        {
            return BadRequest(new
            {
                message = "Cannot deactivate a category that has active products assigned."
            });
        }

        category.IsActive = false;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:int}/restore")]
    public async Task<IActionResult> RestoreCategory(int id)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        if (category.IsActive)
        {
            return BadRequest(new
            {
                message = "Category is already active."
            });
        }

        category.IsActive = true;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}