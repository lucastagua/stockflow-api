using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;
using StockFlow.Api.DTOs;
using StockFlow.Api.Models;
using StockFlow.Api.Helpers;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockMovementsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StockMovementsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponseDto<StockMovementResponseDto>>> GetStockMovements(
        int? productId,
        StockMovementType? type,
        DateTime? from,
        DateTime? to,
        [FromQuery] PaginationParams paginationParams)
    {
        if (paginationParams.PageNumber <= 0)
        {
            return BadRequest(new
            {
                message = "Page number must be greater than zero."
            });
        }

        if (paginationParams.PageSize <= 0 || paginationParams.PageSize > 100)
        {
            return BadRequest(new
            {
                message = "Page size must be between 1 and 100."
            });
        }

        var query = _context.StockMovements
            .Include(s => s.Product)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(s => s.ProductId == productId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(s => s.Type == type.Value);
        }

        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc);

            query = query.Where(s => s.CreatedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc);

            query = query.Where(s => s.CreatedAt < toUtc);
        }

        var totalRecords = await query.CountAsync();

        var movements = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(s => new StockMovementResponseDto
            {
                Id = s.Id,
                ProductId = s.ProductId,
                ProductName = s.Product != null ? s.Product.Name : string.Empty,
                Type = s.Type,
                Quantity = s.Quantity,
                PreviousStock = s.PreviousStock,
                NewStock = s.NewStock,
                Reason = s.Reason,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        var response = new PagedResponseDto<StockMovementResponseDto>
        {
            PageNumber = paginationParams.PageNumber,
            PageSize = paginationParams.PageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)paginationParams.PageSize),
            Data = movements
        };

        return Ok(response);
    }

    [HttpGet("product/{productId:int}")]
    public async Task<ActionResult<PagedResponseDto<StockMovementResponseDto>>> GetMovementsByProduct(
        int productId,
        [FromQuery] PaginationParams paginationParams)
    {
        if (paginationParams.PageNumber <= 0)
        {
            return BadRequest(new
            {
                message = "Page number must be greater than zero."
            });
        }

        if (paginationParams.PageSize <= 0 || paginationParams.PageSize > 100)
        {
            return BadRequest(new
            {
                message = "Page size must be between 1 and 100."
            });
        }

        var productExists = await _context.Products
            .AnyAsync(p => p.Id == productId);

        if (!productExists)
        {
            return NotFound(new
            {
                message = "Product not found."
            });
        }

        var query = _context.StockMovements
            .Include(s => s.Product)
            .Where(s => s.ProductId == productId)
            .AsQueryable();

        var totalRecords = await query.CountAsync();

        var movements = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((paginationParams.PageNumber - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .Select(s => new StockMovementResponseDto
            {
                Id = s.Id,
                ProductId = s.ProductId,
                ProductName = s.Product != null ? s.Product.Name : string.Empty,
                Type = s.Type,
                Quantity = s.Quantity,
                PreviousStock = s.PreviousStock,
                NewStock = s.NewStock,
                Reason = s.Reason,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        var response = new PagedResponseDto<StockMovementResponseDto>
        {
            PageNumber = paginationParams.PageNumber,
            PageSize = paginationParams.PageSize,
            TotalRecords = totalRecords,
            TotalPages = (int)Math.Ceiling(totalRecords / (double)paginationParams.PageSize),
            Data = movements
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<StockMovementResponseDto>> CreateStockMovement(
        CreateStockMovementDto createStockMovementDto)
    {
        if (createStockMovementDto.Quantity <= 0)
        {
            return BadRequest(new
            {
                message = "Quantity must be greater than zero."
            });
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == createStockMovementDto.ProductId);

        if (product is null)
        {
            return NotFound(new
            {
                message = "Product not found."
            });
        }

        var previousStock = product.Stock;
        int newStock;

        switch (createStockMovementDto.Type)
        {
            case StockMovementType.In:
                newStock = previousStock + createStockMovementDto.Quantity;
                break;

            case StockMovementType.Out:
                if (previousStock < createStockMovementDto.Quantity)
                {
                    return BadRequest(new
                    {
                        message = "Not enough stock available."
                    });
                }

                newStock = previousStock - createStockMovementDto.Quantity;
                break;

            case StockMovementType.Adjustment:
                newStock = createStockMovementDto.Quantity;
                break;

            default:
                return BadRequest(new
                {
                    message = "Invalid stock movement type."
                });
        }

        product.Stock = newStock;

        var stockMovement = new StockMovement
        {
            ProductId = product.Id,
            Type = createStockMovementDto.Type,
            Quantity = createStockMovementDto.Quantity,
            PreviousStock = previousStock,
            NewStock = newStock,
            Reason = createStockMovementDto.Reason?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.StockMovements.Add(stockMovement);

        await _context.SaveChangesAsync();

        var response = new StockMovementResponseDto
        {
            Id = stockMovement.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            Type = stockMovement.Type,
            Quantity = stockMovement.Quantity,
            PreviousStock = stockMovement.PreviousStock,
            NewStock = stockMovement.NewStock,
            Reason = stockMovement.Reason,
            CreatedAt = stockMovement.CreatedAt
        };

        return CreatedAtAction(
            nameof(GetMovementsByProduct),
            new { productId = product.Id },
            response
        );
    }
}