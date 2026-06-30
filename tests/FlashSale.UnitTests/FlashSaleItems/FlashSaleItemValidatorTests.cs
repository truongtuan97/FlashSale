using FlashSale.Api.FlashSaleItems;

namespace FlashSale.UnitTests.FlashSaleItems;

public sealed class FlashSaleItemValidatorTests
{
    private static readonly Guid CampaignId = Guid.Parse("84fb7f21-12d8-4b7f-8941-f5c7e6120021");
    private static readonly Guid ProductId = Guid.Parse("3f79280b-f3de-4d28-88ea-8f1f2fe8185d");

    [Fact]
    public void Validate_ReturnsError_WhenCampaignIdIsEmpty()
    {
        var result = FlashSaleItemValidator.Validate(Guid.Empty, ProductId, 100, 80, 10, 0);

        Assert.Equal("Campaign is required.", result);
    }

    [Fact]
    public void Validate_ReturnsError_WhenProductIdIsEmpty()
    {
        var result = FlashSaleItemValidator.Validate(CampaignId, Guid.Empty, 100, 80, 10, 0);

        Assert.Equal("Product is required.", result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ReturnsError_WhenOriginalPriceIsNotPositive(decimal originalPrice)
    {
        var result = FlashSaleItemValidator.Validate(CampaignId, ProductId, originalPrice, 80, 10, 0);

        Assert.Equal("Original price must be greater than 0.", result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ReturnsError_WhenSalePriceIsNotPositive(decimal salePrice)
    {
        var result = FlashSaleItemValidator.Validate(CampaignId, ProductId, 100, salePrice, 10, 0);

        Assert.Equal("Sale price must be greater than 0.", result);
    }

    [Fact]
    public void Validate_ReturnsError_WhenSalePriceIsGreaterThanOriginalPrice()
    {
        var result = FlashSaleItemValidator.Validate(CampaignId, ProductId, 100, 120, 10, 0);

        Assert.Equal("Sale price must be less than or equal to original price.", result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ReturnsError_WhenTotalQuantityIsNotPositive(int totalQuantity)
    {
        var result = FlashSaleItemValidator.Validate(CampaignId, ProductId, 100, 80, totalQuantity, 0);

        Assert.Equal("Total quantity must be greater than 0.", result);
    }

    [Fact]
    public void Validate_ReturnsError_WhenSoldQuantityIsNegative()
    {
        var result = FlashSaleItemValidator.Validate(CampaignId, ProductId, 100, 80, 10, -1);

        Assert.Equal("Sold quantity cannot be negative.", result);
    }

    [Fact]
    public void Validate_ReturnsError_WhenSoldQuantityIsGreaterThanTotalQuantity()
    {
        var result = FlashSaleItemValidator.Validate(CampaignId, ProductId, 100, 80, 10, 11);

        Assert.Equal("Sold quantity cannot be greater than total quantity.", result);
    }

    [Fact]
    public void Validate_ReturnsNull_WhenRequestIsValid()
    {
        var result = FlashSaleItemValidator.Validate(CampaignId, ProductId, 100, 80, 10, 3);

        Assert.Null(result);
    }
}
