using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;
using StockFlow.Api.DTOs;
using StockFlow.Api.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
    {
        var totalProducts = await _context.Products.CountAsync();

        var activeProducts = await _context.Products
            .CountAsync(p => p.IsActive);

        var lowStockProducts = await _context.Products
            .CountAsync(p => p.Stock <= p.MinimumStock);

        var completedSales = await _context.Sales
            .CountAsync(s => s.Status == SaleStatus.Completed);

        var cancelledSales = await _context.Sales
            .CountAsync(s => s.Status == SaleStatus.Cancelled);

        var totalRevenueArs = await _context.Sales
            .Where(s => s.Status == SaleStatus.Completed)
            .SumAsync(s => s.TotalAmountArs);

        var recentSales = await _context.Sales
            .OrderByDescending(s => s.CreatedAt)
            .Take(5)
            .Select(s => new RecentSaleDto
            {
                Id = s.Id,
                CreatedAt = s.CreatedAt,
                TotalAmountArs = s.TotalAmountArs,
                Status = s.Status
            })
            .ToListAsync();

        var recentStockMovements = await _context.StockMovements
            .Include(s => s.Product)
            .OrderByDescending(s => s.CreatedAt)
            .Take(5)
            .Select(s => new RecentStockMovementDto
            {
                Id = s.Id,
                ProductName = s.Product != null ? s.Product.Name : string.Empty,
                Type = s.Type,
                Quantity = s.Quantity,
                PreviousStock = s.PreviousStock,
                NewStock = s.NewStock,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        var response = new DashboardSummaryDto
        {
            TotalProducts = totalProducts,
            ActiveProducts = activeProducts,
            LowStockProducts = lowStockProducts,
            CompletedSales = completedSales,
            CancelledSales = cancelledSales,
            TotalRevenueArs = totalRevenueArs,
            RecentSales = recentSales,
            RecentStockMovements = recentStockMovements
        };

        return Ok(response);
    }
}