using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;
using StockFlow.Api.DTOs;
using StockFlow.Api.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly AppDbContext _context;

    public SalesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SaleResponseDto>>> GetSales()
    {
        var sales = await _context.Sales
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SaleResponseDto
            {
                Id = s.Id,
                CreatedAt = s.CreatedAt,
                TotalAmountArs = s.TotalAmountArs,
                Status = s.Status,
                Items = s.Items.Select(i => new SaleItemResponseDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product != null ? i.Product.Name : string.Empty,
                    Quantity = i.Quantity,
                    UnitPriceArs = i.UnitPriceArs,
                    SubtotalArs = i.SubtotalArs
                }).ToList()
            })
            .ToListAsync();

        return Ok(sales);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SaleResponseDto>> GetSaleById(int id)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .Where(s => s.Id == id)
            .Select(s => new SaleResponseDto
            {
                Id = s.Id,
                CreatedAt = s.CreatedAt,
                TotalAmountArs = s.TotalAmountArs,
                Status = s.Status,
                Items = s.Items.Select(i => new SaleItemResponseDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product != null ? i.Product.Name : string.Empty,
                    Quantity = i.Quantity,
                    UnitPriceArs = i.UnitPriceArs,
                    SubtotalArs = i.SubtotalArs
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (sale is null)
        {
            return NotFound(new
            {
                message = "Sale not found."
            });
        }

        return Ok(sale);
    }

    [HttpPost]
    public async Task<ActionResult<SaleResponseDto>> CreateSale(CreateSaleDto createSaleDto)
    {
        if (createSaleDto.Items.Count == 0)
        {
            return BadRequest(new
            {
                message = "Sale must contain at least one item."
            });
        }

        var productIds = createSaleDto.Items
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        if (productIds.Count != createSaleDto.Items.Count)
        {
            return BadRequest(new
            {
                message = "Duplicated products are not allowed in the same sale."
            });
        }

        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        if (products.Count != productIds.Count)
        {
            return BadRequest(new
            {
                message = "One or more products do not exist."
            });
        }

        var sale = new Sale
        {
            CreatedAt = DateTime.UtcNow,
            Status = SaleStatus.Completed
        };

        decimal totalAmount = 0;

        foreach (var itemDto in createSaleDto.Items)
        {
            if (itemDto.Quantity <= 0)
            {
                return BadRequest(new
                {
                    message = "Item quantity must be greater than zero."
                });
            }

            var product = products.First(p => p.Id == itemDto.ProductId);

            if (!product.IsActive)
            {
                return BadRequest(new
                {
                    message = $"Product '{product.Name}' is inactive."
                });
            }

            if (product.Stock < itemDto.Quantity)
            {
                return BadRequest(new
                {
                    message = $"Not enough stock for product '{product.Name}'. Available stock: {product.Stock}."
                });
            }

            var previousStock = product.Stock;
            var newStock = previousStock - itemDto.Quantity;

            product.Stock = newStock;

            var subtotal = product.PriceArs * itemDto.Quantity;
            totalAmount += subtotal;

            var saleItem = new SaleItem
            {
                ProductId = product.Id,
                Quantity = itemDto.Quantity,
                UnitPriceArs = product.PriceArs,
                SubtotalArs = subtotal
            };

            sale.Items.Add(saleItem);

            var stockMovement = new StockMovement
            {
                ProductId = product.Id,
                Type = StockMovementType.Out,
                Quantity = itemDto.Quantity,
                PreviousStock = previousStock,
                NewStock = newStock,
                Reason = $"Sale created",
                CreatedAt = DateTime.UtcNow
            };

            _context.StockMovements.Add(stockMovement);
        }

        sale.TotalAmountArs = totalAmount;

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync();

        var response = new SaleResponseDto
        {
            Id = sale.Id,
            CreatedAt = sale.CreatedAt,
            TotalAmountArs = sale.TotalAmountArs,
            Status = sale.Status,
            Items = sale.Items.Select(i =>
            {
                var product = products.First(p => p.Id == i.ProductId);

                return new SaleItemResponseDto
                {
                    ProductId = i.ProductId,
                    ProductName = product.Name,
                    Quantity = i.Quantity,
                    UnitPriceArs = i.UnitPriceArs,
                    SubtotalArs = i.SubtotalArs
                };
            }).ToList()
        };

        return CreatedAtAction(
            nameof(GetSaleById),
            new { id = sale.Id },
            response
        );
    }
}