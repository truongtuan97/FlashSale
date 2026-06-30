using FlashSale.Api.FlashSaleCampaigns;
using FlashSale.Domain.Entities;

namespace FlashSale.UnitTests.FlashSaleCampaigns;

public sealed class CampaignResponseTests
{
    [Fact]
    public void FromFlashSaleCampaign_MapFlashSaleCampaignFields()
    {
        var createdAt = new DateTimeOffset(2026, 6, 30, 8, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 6, 30, 9, 0, 0, TimeSpan.Zero);
        var campaign = new FlashSaleCampaign
        {
            Id = Guid.Parse("27e5406f-5f60-4b13-929f-529ffcdad2e4"),
            Name = "FlashSale Campaign No.1",
            IsActive = true,
            StartsAt = createdAt,
            EndsAt = createdAt.AddDays(10),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        var response = FlashSaleCampaignResponse.FromFlashSaleCampaign(campaign);

        Assert.Equal(campaign.Id, response.Id);
        Assert.Equal(campaign.Name, response.Name);
        Assert.Equal(campaign.IsActive, response.IsActive);
        Assert.Equal(createdAt, response.StartsAt);
        Assert.Equal(createdAt.AddDays(10), response.EndsAt);
        Assert.Equal(createdAt, response.CreatedAt);
        Assert.Equal(updatedAt, response.UpdatedAt);
    }    
}
