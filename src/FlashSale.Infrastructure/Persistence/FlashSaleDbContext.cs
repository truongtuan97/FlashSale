using FlashSale.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Infrastructure.Persistence;

public sealed class FlashSaleDbContext : DbContext
{
    public FlashSaleDbContext(DbContextOptions<FlashSaleDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<FlashSaleCampaign> FlashSaleCampaigns { get; set; } = null!;
    public DbSet<FlashSaleItem> FlashSaleItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlashSaleDbContext).Assembly);
    }
}
