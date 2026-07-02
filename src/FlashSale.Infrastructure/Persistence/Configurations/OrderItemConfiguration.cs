using Microsoft.EntityFrameworkCore;
using FlashSale.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlashSale.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.Quantity).IsRequired();

        builder.Property(oi => oi.UnitPrice).IsRequired().HasPrecision(18, 2);
        builder.Property(oi => oi.LineTotal).IsRequired().HasPrecision(18, 2);
        builder.Property(oi => oi.CreatedAt).IsRequired();

        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oi => oi.FlashSaleItem)
            .WithMany(fsi => fsi.OrderItems)
            .HasForeignKey(oi => oi.FlashSaleItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(oi => oi.OrderId);
        builder.HasIndex(oi => oi.FlashSaleItemId);
    }
}