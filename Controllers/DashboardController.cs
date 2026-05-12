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
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(DateTime? from, DateTime? to)
    {
        var salesQuery = _context.Sales.AsQueryable();
        var stockMovementsQuery = _context.StockMovements.AsQueryable();

        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Utc);

            salesQuery = salesQuery.Where(s => s.CreatedAt >= fromUtc);
            stockMovementsQuery = stockMovementsQuery.Where(s => s.CreatedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value.Date.AddDays(1), DateTimeKind.Utc);

            salesQuery = salesQuery.Where(s => s.CreatedAt < toUtc);
            stockMovementsQuery = stockMovementsQuery.Where(s => s.CreatedAt < toUtc);
        }

        var totalProducts = await _context.Products.CountAsync();

        var activeProducts = await _context.Products
            .CountAsync(p => p.IsActive);

        var lowStockProducts = await _context.Products
            .CountAsync(p => p.Stock <= p.MinimumStock);

        var completedSales = await salesQuery
            .CountAsync(s => s.Status == SaleStatus.Completed);

        var cancelledSales = await salesQuery
            .CountAsync(s => s.Status == SaleStatus.Cancelled);

        var totalRevenueArs = await salesQuery
            .Where(s => s.Status == SaleStatus.Completed)
            .SumAsync(s => s.TotalAmountArs);

        var recentSales = await salesQuery
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

        var recentStockMovements = await stockMovementsQuery
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