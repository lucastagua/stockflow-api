using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;
using StockFlow.Api.DTOs;
using StockFlow.Api.Models;
using StockFlow.Api.Helpers;

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
    public async Task<ActionResult<PagedResponseDto<ProductResponseDto>>> GetProducts(
        string? search,
        int? categoryId,
        bool? isActive,
        bool? lowStock,
        [FromQuery] PaginationParams paginationParams)
    {
        if (paginationParams.PageNumber <= 0)
        {
            return BadRequest(new
            {
                message = "Page number must be greater than zero."
            });
        }

        if (paginationParams.PageSize <= 0)
        {
            return BadRequest(new
            {
                message = "Page size must be greater than zero."
            });
        }

        var query = _context.Products
            .Include(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();

            query = query.Where(p =>
                p.Name.ToLower().Contains(normalizedSearch) ||
                (p.Brand != null && p.Brand.ToLower().Contains(normalizedSearch)) ||
                (p.Sku != null && p.Sku.ToLower().Contains(normalizedSearch))
            );
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        if (lowStock.HasValue && lowStock.Value)
        {
            query = query.Where(p => p.Stock <= p.MinimumStock);
        }

        var totalRecords = await query.CountAsync();

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Brand = p.Brand,
                Sku = p.Sku,
                CostUsd = p.CostUsd,
                ProfitMarginPercentage = p.ProfitMarginPercentage,
                PriceArs = p.PriceArs,
                Stock = p.Stock,
                MinimumStock = p.MinimumStock,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty
            })
            .ToListAsync();

        var response = new PagedResponseDto<ProductResponseDto>
        {
            PageNumber = paginationParams.PageNumber,
            PageSize = paginationParams.PageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)paginationParams.PageSize),
            Data = products
        };

        return Ok(response);
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
                ProfitMarginPercentage = p.ProfitMarginPercentage,
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

        if (createProductDto.ProfitMarginPercentage < 0)
        {
            return BadRequest(new
            {
                message = "Profit margin percentage cannot be negative."
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

        var category = await _context.Categories
        .FirstOrDefaultAsync(c => c.Id == createProductDto.CategoryId);

        if (category is null)
        {
            return BadRequest(new
            {
                message = "The selected category does not exist."
            });
        }

        if (!category.IsActive)
        {
            return BadRequest(new
            {
                message = "The selected category is inactive."
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

        var latestExchangeRate = await _context.ExchangeRates
            .OrderByDescending(e => e.Date)
            .FirstOrDefaultAsync();

        if (latestExchangeRate is null)
        {
            return BadRequest(new
            {
                message = "No exchange rate has been registered yet. Please register an exchange rate before creating products."
            });
        }

        var marginMultiplier = 1 + (createProductDto.ProfitMarginPercentage / 100);

        var calculatedPriceArs = Math.Round(
            createProductDto.CostUsd * latestExchangeRate.Value * marginMultiplier,
            2
        );

    var product = new Product
        {
            Name = createProductDto.Name.Trim(),
            Brand = createProductDto.Brand?.Trim(),
            Sku = createProductDto.Sku?.Trim(),
            CostUsd = createProductDto.CostUsd,
            ProfitMarginPercentage = createProductDto.ProfitMarginPercentage,
            PriceArs = calculatedPriceArs,
            Stock = createProductDto.Stock,
            MinimumStock = createProductDto.MinimumStock,
            CategoryId = createProductDto.CategoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var productResponse = new ProductResponseDto
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
            CategoryName = string.Empty
        };

        return CreatedAtAction(
            nameof(GetProductById),
            new { id = product.Id },
            productResponse
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

        if (updateProductDto.ProfitMarginPercentage < 0)
        {
            return BadRequest(new
            {
                message = "Profit margin percentage cannot be negative."
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

        var category = await _context.Categories
        .FirstOrDefaultAsync(c => c.Id == updateProductDto.CategoryId);

        if (category is null)
        {
            return BadRequest(new
            {
                message = "The selected category does not exist."
            });
        }

        if (!category.IsActive)
        {
            return BadRequest(new
            {
                message = "The selected category is inactive."
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

        var latestExchangeRate = await _context.ExchangeRates
            .OrderByDescending(e => e.Date)
            .FirstOrDefaultAsync();

        if (latestExchangeRate is null)
        {
            return BadRequest(new
            {
                message = "No exchange rate has been registered yet. Please register an exchange rate before updating products."
            });
        }

        var marginMultiplier = 1 + (updateProductDto.ProfitMarginPercentage / 100);

        var calculatedPriceArs = Math.Round(
            updateProductDto.CostUsd * latestExchangeRate.Value * marginMultiplier,
            2
        );

        product.Name = updateProductDto.Name.Trim();
        product.Brand = updateProductDto.Brand?.Trim();
        product.Sku = updateProductDto.Sku?.Trim();
        product.CostUsd = updateProductDto.CostUsd;
        product.ProfitMarginPercentage = updateProductDto.ProfitMarginPercentage;
        product.PriceArs = calculatedPriceArs;
        product.Stock = updateProductDto.Stock;
        product.MinimumStock = updateProductDto.MinimumStock;
        product.IsActive = updateProductDto.IsActive;
        product.CategoryId = updateProductDto.CategoryId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("recalculate-prices")]
    public async Task<ActionResult<RecalculatePricesResponseDto>> RecalculatePrices()
    {
        var latestExchangeRate = await _context.ExchangeRates
            .OrderByDescending(e => e.Date)
            .FirstOrDefaultAsync();

        if (latestExchangeRate is null)
        {
            return BadRequest(new
            {
                message = "No exchange rate has been registered yet."
            });
        }

        var products = await _context.Products
            .Where(p => p.IsActive)
            .ToListAsync();

        foreach (var product in products)
        {
            var marginMultiplier = 1 + (product.ProfitMarginPercentage / 100);

            product.PriceArs = Math.Round(
                product.CostUsd * latestExchangeRate.Value * marginMultiplier,
                2
            );
        }

        await _context.SaveChangesAsync();

        var response = new RecalculatePricesResponseDto
        {
            UpdatedProducts = products.Count,
            ExchangeRate = latestExchangeRate.Value,
            ExchangeRateDate = latestExchangeRate.Date
        };

        return Ok(response);
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetLowStockProducts()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.Stock <= p.MinimumStock)
            .OrderBy(p => p.Stock)
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                Name = p.Name,
                Brand = p.Brand,
                Sku = p.Sku,
                CostUsd = p.CostUsd,
                ProfitMarginPercentage = p.ProfitMarginPercentage,
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

        if (!product.IsActive)
        {
            return BadRequest(new
            {
                message = "Product is already inactive."
            });
        }

        product.IsActive = false;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:int}/restore")]
    public async Task<IActionResult> RestoreProduct(int id)
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

        if (product.IsActive)
        {
            return BadRequest(new
            {
                message = "Product is already active."
            });
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == product.CategoryId);

        if (category is null)
        {
            return BadRequest(new
            {
                message = "Product category does not exist."
            });
        }

        if (!category.IsActive)
        {
            return BadRequest(new
            {
                message = "Cannot restore product because its category is inactive."
            });
        }

        product.IsActive = true;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}