using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FlashSale.IntegrationTests.Api;

public sealed class ProductApiTests
{
    [Fact]
    public async Task Can_create_update_and_delete_product()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Integration Test Product",
            description = "Created by product integration test.",
            price = 199.99m,
            imageUrl = "https://example.com/product.png",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdProduct = await ReadJsonAsync(createResponse);
        var productId = createdProduct.RootElement.GetProperty("id").GetGuid();

        var updateResponse = await client.PutAsJsonAsync($"/api/products/{productId}", new
        {
            name = "Updated Integration Test Product",
            description = "Updated by product integration test.",
            price = 149.99m,
            imageUrl = "https://example.com/product-updated.png",
            isActive = false
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedProduct = await ReadJsonAsync(updateResponse);
        Assert.Equal("Updated Integration Test Product", updatedProduct.RootElement.GetProperty("name").GetString());
        Assert.Equal(149.99m, updatedProduct.RootElement.GetProperty("price").GetDecimal());
        Assert.False(updatedProduct.RootElement.GetProperty("isActive").GetBoolean());

        var getResponse = await client.GetAsync($"/api/products/{productId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/products/{productId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getDeletedResponse = await client.GetAsync($"/api/products/{productId}");

        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
