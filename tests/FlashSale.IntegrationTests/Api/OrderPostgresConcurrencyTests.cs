using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FlashSale.IntegrationTests.Api;

public sealed class OrderPostgresConcurrencyTests
{
    [Fact]
    public async Task Concurrent_orders_allow_only_one_customer_to_buy_final_stock_unit()
    {
        await using var factory = new PostgresFlashSaleApiFactory();

        using var client1 = factory.CreateClient();
        using var client2 = factory.CreateClient();

        var flashSaleItemId = await CreateFlashSaleItemAsync(
            client1,
            totalQuantity: 1,
            soldQuantity: 0,
            salePrice: 80m);

        var request1 = PostOrderAsync(client1, new
        {
            customerName = "Customer One",
            customerEmail = "one@example.com",
            items = new[]
            {
                new
                {
                    flashSaleItemId,
                    quantity = 1
                }
            }
        });

        var request2 = PostOrderAsync(client2, new
        {
            customerName = "Customer Two",
            customerEmail = "two@example.com",
            items = new[]
            {
                new
                {
                    flashSaleItemId,
                    quantity = 1
                }
            }
        });

        var responses = await Task.WhenAll(request1, request2);

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.BadRequest));

        var failedResponse = responses.Single(response => response.StatusCode == HttpStatusCode.BadRequest);
        var error = await ReadJsonAsync(failedResponse);
        Assert.Equal("Not enough stock.", error.RootElement.GetProperty("message").GetString());

        var itemResponse = await client1.GetAsync($"/api/flash-sale-items/{flashSaleItemId}");
        Assert.Equal(HttpStatusCode.OK, itemResponse.StatusCode);

        var item = await ReadJsonAsync(itemResponse);
        Assert.Equal(1, item.RootElement.GetProperty("soldQuantity").GetInt32());
        Assert.Equal(0, item.RootElement.GetProperty("availableQuantity").GetInt32());
    }

    [Fact]
    public async Task Create_order_returns_bad_request_when_duplicate_lines_exceed_available_stock()
    {
        await using var factory = new PostgresFlashSaleApiFactory();
        using var client = factory.CreateClient();

        var flashSaleItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 5,
            soldQuantity: 0,
            salePrice: 80m
        );

        var response = await PostOrderAsync(client, new
        {
            customerName = "Duplicate Line Customer",
            customerEmail = "duplicate@example.com",
            items = new[]
            {
                new
                {
                    flashSaleItemId,
                    quantity = 3
                },
                new
                {
                    flashSaleItemId,
                    quantity = 3
                }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadJsonAsync(response);
        Assert.Equal("Not enough stock.", error.RootElement.GetProperty("message").GetString());
        var itemResponse = await client.GetAsync($"/api/flash-sale-items/{flashSaleItemId}");
        Assert.Equal(HttpStatusCode.OK, itemResponse.StatusCode);

        var item = await ReadJsonAsync(itemResponse);
        Assert.Equal(0, item.RootElement.GetProperty("soldQuantity").GetInt32());
        Assert.Equal(5, item.RootElement.GetProperty("availableQuantity").GetInt32());
    }

    [Fact]
    public async Task Create_order_rolls_back_stock_updates_when_one_item_has_insufficient_stock()
    {
        await using var factory = new PostgresFlashSaleApiFactory();
        using var client = factory.CreateClient();

        var enoughStockItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 10,
            soldQuantity: 0,
            salePrice: 80m
        );
        var insufficientStockItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 2,
            soldQuantity: 1,
            salePrice: 50m
        );

        var response = await PostOrderAsync(client, new
        {
            customerName = "Rollback Customer",
            customerEmail = "rollback@example.com",
            items = new[]
            {
                new
                {
                    flashSaleItemId = enoughStockItemId,
                    quantity = 3
                },
                new
                {
                    flashSaleItemId = insufficientStockItemId,
                    quantity = 2
                }
            }
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await ReadJsonAsync(response);
        Assert.Equal("Not enough stock.", error.RootElement.GetProperty("message").GetString());

        var enoughStock = await GetFlashSaleItemAsync(client, enoughStockItemId);
        Assert.Equal(0, enoughStock.RootElement.GetProperty("soldQuantity").GetInt32());
        Assert.Equal(10, enoughStock.RootElement.GetProperty("availableQuantity").GetInt32());

        var insufficientStockItem = await GetFlashSaleItemAsync(client, insufficientStockItemId);
        Assert.Equal(1, insufficientStockItem.RootElement.GetProperty("soldQuantity").GetInt32());
        Assert.Equal(1, insufficientStockItem.RootElement.GetProperty("availableQuantity").GetInt32());
    }

    [Fact]
    public async Task Create_order_increases_stock_for_every_item_when_multi_item_order_succeeds()
    {
        await using var factory = new PostgresFlashSaleApiFactory();
        using var client = factory.CreateClient();

        var firstItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 10,
            soldQuantity: 1,
            salePrice: 80m
        );
        var secondItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 8,
            soldQuantity: 2,
            salePrice: 50m
        );

        var response = await PostOrderAsync(client, new
        {
            customerName = "Multi Item Customer",
            customerEmail = "multi@example.com",
            items = new[]
            {
                new
                {
                    flashSaleItemId = firstItemId,
                    quantity = 3
                },
                new
                {
                    flashSaleItemId = secondItemId,
                    quantity = 2
                }
            }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var order = await ReadJsonAsync(response);
        Assert.Equal(340m, order.RootElement.GetProperty("totalAmount").GetDecimal());

        var items = order.RootElement.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(2, items.Length);

        Assert.Contains(items, item =>
            item.GetProperty("flashSaleItemId").GetGuid() == firstItemId &&
            item.GetProperty("quantity").GetInt32() == 3 &&
            item.GetProperty("unitPrice").GetDecimal() == 80m &&
            item.GetProperty("lineTotal").GetDecimal() == 240m);

        Assert.Contains(items, item =>
            item.GetProperty("flashSaleItemId").GetGuid() == secondItemId &&
            item.GetProperty("quantity").GetInt32() == 2 &&
            item.GetProperty("unitPrice").GetDecimal() == 50m &&
            item.GetProperty("lineTotal").GetDecimal() == 100m);

        var firstItem = await GetFlashSaleItemAsync(client, firstItemId);
        Assert.Equal(4, firstItem.RootElement.GetProperty("soldQuantity").GetInt32());
        Assert.Equal(6, firstItem.RootElement.GetProperty("availableQuantity").GetInt32());

        var secondItem = await GetFlashSaleItemAsync(client, secondItemId);
        Assert.Equal(4, secondItem.RootElement.GetProperty("soldQuantity").GetInt32());
        Assert.Equal(4, secondItem.RootElement.GetProperty("availableQuantity").GetInt32());
    }

    private static async Task<JsonDocument> GetFlashSaleItemAsync(HttpClient client, Guid flashSaleItemId)
    {
        var response = await client.GetAsync($"/api/flash-sale-items/{flashSaleItemId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadJsonAsync(response);
    }

    private static async Task<HttpResponseMessage> PostOrderAsync(
        HttpClient client,
        object request,
        string? idempotencyKey = null)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/orders");
        httpRequest.Headers.Add(
            "Idempotency-Key",
            idempotencyKey ?? Guid.NewGuid().ToString("N"));
        httpRequest.Content = JsonContent.Create(request);

        return await client.SendAsync(httpRequest);
    }

    private static async Task<Guid> CreateFlashSaleItemAsync(
        HttpClient client,
        int totalQuantity,
        int soldQuantity,
        decimal salePrice)
    {
        var productId = await CreateProductAsync(client);
        var campaignId = await CreateCampaignAsync(client);

        var response = await client.PostAsJsonAsync("/api/flash-sale-items", new
        {
            campaignId,
            productId,
            originalPrice = 100m,
            salePrice,
            totalQuantity,
            soldQuantity,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var item = await ReadJsonAsync(response);
        return item.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = $"Postgres Order Test Product {Guid.NewGuid()}",
            description = "Created by PostgreSQL order integration test.",
            price = 100m,
            imageUrl = "https://example.com/postgres-order-product.png",
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
            name = $"Postgres Order Test Campaign {Guid.NewGuid()}",
            startsAt = DateTimeOffset.UtcNow.AddHours(-1),
            endsAt = DateTimeOffset.UtcNow.AddHours(1),
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var campaign = await ReadJsonAsync(response);
        return campaign.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
