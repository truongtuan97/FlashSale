using FlashSale.Api.FlashSaleItems;
using FlashSale.Domain.Entities;

namespace FlashSale.UnitTests.FlashSaleItems;

public sealed class FlashSaleItemResponseTests
{
    [Fact]
    public void FromFlashSaleItem_MapsFlashSaleItemFields()
    {
        var createdAt = new DateTimeOffset(2026, 6, 30, 8, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 6, 30, 9, 0, 0, TimeSpan.Zero);
        var campaignId = Guid.Parse("84fb7f21-12d8-4b7f-8941-f5c7e6120021");
        var productId = Guid.Parse("3f79280b-f3de-4d28-88ea-8f1f2fe8185d");
        var item = new FlashSaleItem
        {
            Id = Guid.Parse("1f422cc3-c914-4f4b-bf23-e026a5df7da0"),
            CampaignId = campaignId,
            Campaign = new FlashSaleCampaign
            {
                Id = campaignId,
                Name = "7.7 Flash Sale"
            },
            ProductId = productId,
            Product = new Product
            {
                Id = productId,
                Name = "Mechanical Keyboard"
            },
            OriginalPrice = 129.99m,
            SalePrice = 89.99m,
            TotalQuantity = 100,
            SoldQuantity = 25,
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        var response = FlashSaleItemResponse.FromFlashSaleItem(item);

        Assert.Equal(item.Id, response.Id);
        Assert.Equal(campaignId, response.CampaignId);
        Assert.Equal("7.7 Flash Sale", response.CampaignName);
        Assert.Equal(productId, response.ProductId);
        Assert.Equal("Mechanical Keyboard", response.ProductName);
        Assert.Equal(129.99m, response.OriginalPrice);
        Assert.Equal(89.99m, response.SalePrice);
        Assert.Equal(100, response.TotalQuantity);
        Assert.Equal(25, response.SoldQuantity);
        Assert.Equal(75, response.AvailableQuantity);
        Assert.True(response.IsActive);
        Assert.Equal(createdAt, response.CreatedAt);
        Assert.Equal(updatedAt, response.UpdatedAt);
    }
}
