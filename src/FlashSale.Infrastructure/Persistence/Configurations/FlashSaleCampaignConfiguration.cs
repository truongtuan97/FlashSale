using FlashSale.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlashSale.Infrastructure.Persistence.Configurations;

public sealed class FlashSaleCampaignConfiguration : IEntityTypeConfiguration<FlashSaleCampaign>
{
    public void Configure(EntityTypeBuilder<FlashSaleCampaign> builder)
    {
        builder.ToTable("FlashSaleCampaigns");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.StartsAt).IsRequired();
        builder.Property(c => c.EndsAt).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        builder.HasMany(c => c.Items)
            .WithOne(i => i.Campaign)
            .HasForeignKey(i => i.CampaignId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
