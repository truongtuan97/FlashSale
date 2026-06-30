using FlashSale.Api.Products;

namespace FlashSale.UnitTests.Products;

public sealed class ProductRequestValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Validate_ReturnsError_WhenNameIsMissing(string name)
    {
        var result = ProductRequestValidator.Validate(name, 100);

        Assert.Equal("Product name is required.", result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ReturnsError_WhenPriceIsNotPositive(decimal price)
    {
        var result = ProductRequestValidator.Validate("Keyboard", price);

        Assert.Equal("Product price must be greater than 0.", result);
    }

    [Fact]
    public void Validate_ReturnsNull_WhenRequestIsValid()
    {
        var result = ProductRequestValidator.Validate("Keyboard", 100);

        Assert.Null(result);
    }
}
