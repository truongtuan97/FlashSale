using FlashSale.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlashSale.Infrastructure.Persistence.Configurations;

public sealed class FlashSaleItemConfiguration : IEntityTypeConfiguration<FlashSaleItem>
{
    public void Configure(EntityTypeBuilder<FlashSaleItem> builder)
    {
        builder.ToTable("FlashSaleItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.OriginalPrice).IsRequired().HasPrecision(18, 2);
        builder.Property(i => i.SalePrice).IsRequired().HasPrecision(18, 2);
        builder.Property(i => i.TotalQuantity).IsRequired();
        builder.Property(i => i.SoldQuantity).IsRequired();
        builder.Property(i => i.IsActive).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired();

        builder.HasOne(i => i.Product)
            .WithMany(p => p.FlashSaleItems)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Campaign)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CampaignId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.CampaignId, i.ProductId }).IsUnique();
    }
}
