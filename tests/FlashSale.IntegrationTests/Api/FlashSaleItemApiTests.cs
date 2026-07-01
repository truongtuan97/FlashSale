using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FlashSale.IntegrationTests.Api;

public sealed class FlashSaleItemApiTests
{
    [Fact]
    public async Task Can_create_flash_sale_item_for_product_and_campaign()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var productResponse = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Integration Test Product",
            description = "Created by API integration test.",
            price = 299.99m,
            imageUrl = "https://example.com/integration-product.png",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        var product = await ReadJsonAsync(productResponse);
        var productId = product.RootElement.GetProperty("id").GetGuid();

        var campaignResponse = await client.PostAsJsonAsync("/api/flash-sale-campaigns", new
        {
            name = "Integration Test Campaign",
            startsAt = DateTimeOffset.UtcNow,
            endsAt = DateTimeOffset.UtcNow.AddDays(1),
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Created, campaignResponse.StatusCode);
        var campaign = await ReadJsonAsync(campaignResponse);
        var campaignId = campaign.RootElement.GetProperty("id").GetGuid();

        var itemResponse = await client.PostAsJsonAsync("/api/flash-sale-items", new
        {
            campaignId,
            productId,
            originalPrice = 299.99m,
            salePrice = 199.99m,
            totalQuantity = 50,
            soldQuantity = 8,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Created, itemResponse.StatusCode);
        var item = await ReadJsonAsync(itemResponse);
        var itemId = item.RootElement.GetProperty("id").GetGuid();

        var fetchedResponse = await client.GetAsync($"/api/flash-sale-items/{itemId}");

        Assert.Equal(HttpStatusCode.OK, fetchedResponse.StatusCode);
        var fetchedItem = await ReadJsonAsync(fetchedResponse);
        Assert.Equal(42, fetchedItem.RootElement.GetProperty("availableQuantity").GetInt32());
        Assert.Equal(productId, fetchedItem.RootElement.GetProperty("productId").GetGuid());
        Assert.Equal(campaignId, fetchedItem.RootElement.GetProperty("campaignId").GetGuid());
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
