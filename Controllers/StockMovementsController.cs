using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;
using StockFlow.Api.DTOs;
using StockFlow.Api.Models;

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
    public async Task<ActionResult<IEnumerable<StockMovementResponseDto>>> GetStockMovements()
    {
        var movements = await _context.StockMovements
            .Include(s => s.Product)
            .OrderByDescending(s => s.CreatedAt)
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

        return Ok(movements);
    }

    [HttpGet("product/{productId:int}")]
    public async Task<ActionResult<IEnumerable<StockMovementResponseDto>>> GetMovementsByProduct(int productId)
    {
        var productExists = await _context.Products
            .AnyAsync(p => p.Id == productId);

        if (!productExists)
        {
            return NotFound(new
            {
                message = "Product not found."
            });
        }

        var movements = await _context.StockMovements
            .Include(s => s.Product)
            .Where(s => s.ProductId == productId)
            .OrderByDescending(s => s.CreatedAt)
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

        return Ok(movements);
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