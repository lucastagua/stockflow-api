using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace StockFlow.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(p => p.Brand)
                .HasMaxLength(100);

            entity.Property(p => p.Sku)
                .HasMaxLength(50);

            entity.Property(p => p.CostUsd)
                .HasPrecision(18, 2);

            entity.Property(p => p.ProfitMarginPercentage)
                .HasPrecision(5, 2);

            entity.Property(p => p.PriceArs)
                .HasPrecision(18, 2);

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);
        });

        modelBuilder.Entity<ExchangeRate>(entity =>
        {
            entity.Property(e => e.Value)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.Property(s => s.Reason)
                .HasMaxLength(250);

            entity.HasOne(s => s.Product)
                .WithMany(p => p.StockMovements)
                .HasForeignKey(s => s.ProductId);
        });
    }
}