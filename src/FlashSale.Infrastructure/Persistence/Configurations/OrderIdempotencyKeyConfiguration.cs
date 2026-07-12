using FlashSale.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlashSale.Infrastructure.Persistence.Configurations;

public sealed class OrderIdempotencyKeyConfiguration : IEntityTypeConfiguration<OrderIdempotencyKey>
{
    public void Configure(EntityTypeBuilder<OrderIdempotencyKey> builder)
    {
        builder.HasKey(key => key.Id);

        builder.Property(key => key.Key).HasMaxLength(200).IsRequired();

        builder.Property(key => key.RequestHash).HasMaxLength(128).IsRequired();

        builder.HasIndex(key => key.Key).IsUnique();

        builder.HasOne(key => key.Order).WithMany().HasForeignKey(key => key.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
