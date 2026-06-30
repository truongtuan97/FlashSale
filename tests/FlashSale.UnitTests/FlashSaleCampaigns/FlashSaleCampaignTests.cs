using FlashSale.Api.FlashSaleCampaigns;

namespace FlashSale.UnitTests.FlashSaleCampaigns;

public sealed class CampaignRequestValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Validate_ReturnsError_WhenNameIsMissing(string name)
    {
        var result = FlashSaleCampaignValidator.Validate(name, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(10));

        Assert.Equal("Campaign name is required.", result);
    }

    [Fact]
    public void Validate_ReturnsError_WhenEndAtIsNotLaterThanStartAt()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start;

        var result = FlashSaleCampaignValidator.Validate("ValidName", start, end);

        Assert.Equal("Campaign end time must be later than start time.", result);
    }

    [Fact]
    public void Validate_ReturnsNull_WhenRequestIsValid()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddHours(2);

        var result = FlashSaleCampaignValidator.Validate("ValidName", start, end);

        Assert.Null(result);
    }
}
