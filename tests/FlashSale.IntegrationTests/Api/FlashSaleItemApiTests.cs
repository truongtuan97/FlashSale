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

    [Fact]
    public async Task Create_flash_sale_item_returns_bad_request_when_campaign_does_not_exist()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);

        var response = await client.PostAsJsonAsync("/api/flash-sale-items", new
        {
            campaignId = Guid.NewGuid(),
            productId,
            originalPrice = 299.99m,
            salePrice = 199.99m,
            totalQuantity = 50,
            soldQuantity = 0,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadJsonAsync(response);
        Assert.Equal("Campaign does not exist.", error.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_flash_sale_item_returns_bad_request_when_product_does_not_exist()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();
        var campaignId = await CreateCampaignAsync(client);

        var response = await client.PostAsJsonAsync("/api/flash-sale-items", new
        {
            campaignId,
            productId = Guid.NewGuid(),
            originalPrice = 299.99m,
            salePrice = 199.99m,
            totalQuantity = 50,
            soldQuantity = 0,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadJsonAsync(response);
        Assert.Equal("Product does not exist.", error.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_flash_sale_item_returns_bad_request_when_sale_price_exceeds_original_price()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var campaignId = await CreateCampaignAsync(client);

        var response = await client.PostAsJsonAsync("/api/flash-sale-items", new
        {
            campaignId,
            productId,
            originalPrice = 199.99m,
            salePrice = 299.99m,
            totalQuantity = 50,
            soldQuantity = 0,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadJsonAsync(response);
        Assert.Equal("Sale price must be less than or equal to original price.", error.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_flash_sale_item_returns_conflict_when_product_already_exists_in_campaign()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();
        var productId = await CreateProductAsync(client);
        var campaignId = await CreateCampaignAsync(client);

        var firstResponse = await CreateFlashSaleItemAsync(client, campaignId, productId);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await CreateFlashSaleItemAsync(client, campaignId, productId);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        var error = await ReadJsonAsync(duplicateResponse);
        Assert.Equal("Product already exists in this campaign.", error.RootElement.GetProperty("message").GetString());
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = $"Integration Test Product {Guid.NewGuid()}",
            description = "Created by flash sale item integration test.",
            price = 299.99m,
            imageUrl = "https://example.com/integration-product.png",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await ReadJsonAsync(response);
        return product.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateCampaignAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/flash-sale-campaigns", new
        {
            name = $"Integration Test Campaign {Guid.NewGuid()}",
            startsAt = DateTimeOffset.UtcNow,
            endsAt = DateTimeOffset.UtcNow.AddDays(1),
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var campaign = await ReadJsonAsync(response);
        return campaign.RootElement.GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> CreateFlashSaleItemAsync(
        HttpClient client,
        Guid campaignId,
        Guid productId)
    {
        return client.PostAsJsonAsync("/api/flash-sale-items", new
        {
            campaignId,
            productId,
            originalPrice = 299.99m,
            salePrice = 199.99m,
            totalQuantity = 50,
            soldQuantity = 0,
            isActive = true
        });
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
