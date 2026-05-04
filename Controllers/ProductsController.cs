using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;
using StockFlow.Api.DTOs;
using StockFlow.Api.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProducts()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .OrderBy(p => p.Name)
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Brand = p.Brand,
                Sku = p.Sku,
                CostUsd = p.CostUsd,
                PriceArs = p.PriceArs,
                Stock = p.Stock,
                MinimumStock = p.MinimumStock,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponseDto>> GetProductById(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.Id == id)
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Brand = p.Brand,
                Sku = p.Sku,
                CostUsd = p.CostUsd,
                PriceArs = p.PriceArs,
                Stock = p.Stock,
                MinimumStock = p.MinimumStock,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty
            })
            .FirstOrDefaultAsync();

        if (product is null)
        {
            return NotFound(new
            {
                message = "Product not found."
            });
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(CreateProductDto createProductDto)
    {
        if (string.IsNullOrWhiteSpace(createProductDto.Name))
        {
            return BadRequest(new
            {
                message = "Product name is required."
            });
        }

        if (createProductDto.CostUsd < 0)
        {
            return BadRequest(new
            {
                message = "Cost in USD cannot be negative."
            });
        }

        if (createProductDto.PriceArs < 0)
        {
            return BadRequest(new
            {
                message = "Price in ARS cannot be negative."
            });
        }

        if (createProductDto.Stock < 0)
        {
            return BadRequest(new
            {
                message = "Stock cannot be negative."
            });
        }

        if (createProductDto.MinimumStock < 0)
        {
            return BadRequest(new
            {
                message = "Minimum stock cannot be negative."
            });
        }

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == createProductDto.CategoryId);

        if (!categoryExists)
        {
            return BadRequest(new
            {
                message = "The selected category does not exist."
            });
        }

        if (!string.IsNullOrWhiteSpace(createProductDto.Sku))
        {
            var skuExists = await _context.Products
                .AnyAsync(p => p.Sku != null &&
                               p.Sku.ToLower() == createProductDto.Sku.ToLower());

            if (skuExists)
            {
                return Conflict(new
                {
                    message = "A product with this SKU already exists."
                });
            }
        }

        var product = new Product
        {
            Name = createProductDto.Name.Trim(),
            Brand = createProductDto.Brand?.Trim(),
            Sku = createProductDto.Sku?.Trim(),
            CostUsd = createProductDto.CostUsd,
            PriceArs = createProductDto.PriceArs,
            Stock = createProductDto.Stock,
            MinimumStock = createProductDto.MinimumStock,
            CategoryId = createProductDto.CategoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetProductById),
            new { id = product.Id },
            product
        );
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, UpdateProductDto updateProductDto)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound(new
            {
                message = "Product not found."
            });
        }

        if (string.IsNullOrWhiteSpace(updateProductDto.Name))
        {
            return BadRequest(new
            {
                message = "Product name is required."
            });
        }

        if (updateProductDto.CostUsd < 0)
        {
            return BadRequest(new
            {
                message = "Cost in USD cannot be negative."
            });
        }

        if (updateProductDto.PriceArs < 0)
        {
            return BadRequest(new
            {
                message = "Price in ARS cannot be negative."
            });
        }

        if (updateProductDto.Stock < 0)
        {
            return BadRequest(new
            {
                message = "Stock cannot be negative."
            });
        }

        if (updateProductDto.MinimumStock < 0)
        {
            return BadRequest(new
            {
                message = "Minimum stock cannot be negative."
            });
        }

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == updateProductDto.CategoryId);

        if (!categoryExists)
        {
            return BadRequest(new
            {
                message = "The selected category does not exist."
            });
        }

        if (!string.IsNullOrWhiteSpace(updateProductDto.Sku))
        {
            var skuExists = await _context.Products
                .AnyAsync(p =>
                    p.Id != id &&
                    p.Sku != null &&
                    p.Sku.ToLower() == updateProductDto.Sku.ToLower()
                );

            if (skuExists)
            {
                return Conflict(new
                {
                    message = "Another product with this SKU already exists."
                });
            }
        }

        product.Name = updateProductDto.Name.Trim();
        product.Brand = updateProductDto.Brand?.Trim();
        product.Sku = updateProductDto.Sku?.Trim();
        product.CostUsd = updateProductDto.CostUsd;
        product.PriceArs = updateProductDto.PriceArs;
        product.Stock = updateProductDto.Stock;
        product.MinimumStock = updateProductDto.MinimumStock;
        product.IsActive = updateProductDto.IsActive;
        product.CategoryId = updateProductDto.CategoryId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound(new
            {
                message = "Product not found."
            });
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}