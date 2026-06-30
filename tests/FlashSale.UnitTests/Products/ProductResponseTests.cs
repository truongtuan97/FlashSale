using FlashSale.Api.Products;
using FlashSale.Domain.Entities;

namespace FlashSale.UnitTests.Products;

public sealed class ProductResponseTests
{
    [Fact]
    public void FromProduct_MapsProductFields()
    {
        var createdAt = new DateTimeOffset(2026, 6, 30, 8, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 6, 30, 9, 0, 0, TimeSpan.Zero);
        var product = new Product
        {
            Id = Guid.Parse("27e5406f-5f60-4b13-929f-529ffcdad2e4"),
            Name = "Mechanical Keyboard",
            Description = "Hot-swappable keyboard",
            Price = 129.99m,
            ImageUrl = "https://example.com/keyboard.png",
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        var response = ProductResponse.FromProduct(product);

        Assert.Equal(product.Id, response.Id);
        Assert.Equal(product.Name, response.Name);
        Assert.Equal(product.Description, response.Description);
        Assert.Equal(product.Price, response.Price);
        Assert.Equal(product.ImageUrl, response.ImageUrl);
        Assert.Equal(product.IsActive, response.IsActive);
        Assert.Equal(createdAt, response.CreatedAt);
        Assert.Equal(updatedAt, response.UpdatedAt);
    }
}
