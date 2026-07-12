using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FlashSale.IntegrationTests.Api;

public sealed class OrderApiTests
{
    [Fact]
    public async Task Can_create_order_for_active_flash_sale_item()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var flashSaleItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 10,
            soldQuantity: 2,
            salePrice: 80m);

        var orderResponse = await PostOrderAsync(client, new
        {
            customerName = "Integration Customer",
            customerEmail = "customer@example.com",
            items = new[]
            {
                new
                {
                    flashSaleItemId,
                    quantity = 3
                }
            }
        });

        Assert.Equal(HttpStatusCode.Created, orderResponse.StatusCode);

        var order = await ReadJsonAsync(orderResponse);
        Assert.Equal("Integration Customer", order.RootElement.GetProperty("customerName").GetString());
        Assert.Equal("customer@example.com", order.RootElement.GetProperty("customerEmail").GetString());
        Assert.Equal(240m, order.RootElement.GetProperty("totalAmount").GetDecimal());
        Assert.Equal(1, order.RootElement.GetProperty("status").GetInt32());

        var items = order.RootElement.GetProperty("items");
        Assert.Single(items.EnumerateArray());
        Assert.Equal(flashSaleItemId, items[0].GetProperty("flashSaleItemId").GetGuid());
        Assert.Equal("Order Test Product", items[0].GetProperty("productName").GetString()![..18]);
        Assert.Equal("Order Test Campaign", items[0].GetProperty("campaignName").GetString()![..19]);
        Assert.Equal(3, items[0].GetProperty("quantity").GetInt32());
        Assert.Equal(80m, items[0].GetProperty("unitPrice").GetDecimal());
        Assert.Equal(240m, items[0].GetProperty("lineTotal").GetDecimal());

        var location = orderResponse.Headers.Location;
        Assert.NotNull(location);

        var getOrderResponse = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, getOrderResponse.StatusCode);

        var savedOrder = await ReadJsonAsync(getOrderResponse);
        Assert.Equal(order.RootElement.GetProperty("id").GetGuid(), savedOrder.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(240m, savedOrder.RootElement.GetProperty("totalAmount").GetDecimal());
        Assert.Single(savedOrder.RootElement.GetProperty("items").EnumerateArray());

        var itemResponse = await client.GetAsync($"/api/flash-sale-items/{flashSaleItemId}");
        Assert.Equal(HttpStatusCode.OK, itemResponse.StatusCode);

        var item = await ReadJsonAsync(itemResponse);
        Assert.Equal(5, item.RootElement.GetProperty("soldQuantity").GetInt32());
        Assert.Equal(5, item.RootElement.GetProperty("availableQuantity").GetInt32());
    }

    [Fact]
    public async Task Get_order_returns_not_found_when_order_does_not_exist()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_order_returns_bad_request_when_quantity_exceeds_available_stock()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var flashSaleItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 5,
            soldQuantity: 4,
            salePrice: 80m);

        var orderResponse = await PostOrderAsync(client, new
        {
            customerName = "Integration Customer",
            customerEmail = "customer@example.com",
            items = new[]
            {
                new
                {
                    flashSaleItemId,
                    quantity = 2
                }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, orderResponse.StatusCode);

        var error = await ReadJsonAsync(orderResponse);
        Assert.Equal("Not enough stock.", error.RootElement.GetProperty("message").GetString());

        var itemResponse = await client.GetAsync($"/api/flash-sale-items/{flashSaleItemId}");
        Assert.Equal(HttpStatusCode.OK, itemResponse.StatusCode);

        var item = await ReadJsonAsync(itemResponse);
        Assert.Equal(4, item.RootElement.GetProperty("soldQuantity").GetInt32());
        Assert.Equal(1, item.RootElement.GetProperty("availableQuantity").GetInt32());
    }

    [Fact]
    public async Task Create_order_returns_bad_request_when_customer_name_is_missing()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var flashSaleItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 10,
            soldQuantity: 0,
            salePrice: 80m);

        var response = await PostOrderAsync(client, new
        {
            customerName = "",
            customerEmail = "customer@example.com",
            items = new[]
            {
            new
            {
                flashSaleItemId,
                quantity = 1
            }
        }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadJsonAsync(response);
        Assert.Equal("Customer name is required.", error.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_order_returns_bad_request_when_customer_email_is_missing()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var flashSaleItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 10,
            soldQuantity: 0,
            salePrice: 80m);

        var response = await PostOrderAsync(client, new
        {
            customerName = "Integration Customer",
            customerEmail = "",
            items = new[]
            {
                new
                {
                    flashSaleItemId,
                    quantity = 1
                }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadJsonAsync(response);
        Assert.Equal("Customer email is required.", error.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_order_returns_bad_request_when_items_are_empty()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var response = await PostOrderAsync(client, new
        {
            customerName = "Integration Customer",
            customerEmail = "customer@example.com",
            items = Array.Empty<object>()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadJsonAsync(response);
        Assert.Equal("Order must contain at least one item.", error.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_order_returns_bad_request_when_flash_sale_item_id_is_missing()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var response = await PostOrderAsync(client, new
        {
            customerName = "Integration Customer",
            customerEmail = "customer@example.com",
            items = new[]
            {
                new
                {
                    flashSaleItemId = Guid.Empty,
                    quantity = 1
                }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadJsonAsync(response);
        Assert.Equal("Flash sale item is required.", error.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_order_returns_bad_request_when_quantity_is_not_positive()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var flashSaleItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 10,
            soldQuantity: 0,
            salePrice: 80m);

        var response = await PostOrderAsync(client, new
        {
            customerName = "Integration Customer",
            customerEmail = "customer@example.com",
            items = new[]
            {
                new
                {
                    flashSaleItemId,
                    quantity = 0
                }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadJsonAsync(response);
        Assert.Equal("Quantity must be greater than 0.", error.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_order_returns_bad_request_when_flash_sale_item_does_not_exist()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var response = await PostOrderAsync(client, new
        {
            customerName = "Integration Customer",
            customerEmail = "customer@example.com",
            items = new[]
            {
                new
                {
                    flashSaleItemId = Guid.NewGuid(),
                    quantity = 1
                }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadJsonAsync(response);
        Assert.Equal("Flash sale item does not exist.", error.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_order_returns_bad_request_when_flash_sale_item_is_inactive()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var flashSaleItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 10,
            soldQuantity: 0,
            salePrice: 80m,
            isActive: false);

        var response = await PostOrderAsync(client, new
        {
            customerName = "Integration Customer",
            customerEmail = "customer@example.com",
            items = new[]
            {
                new
                {
                    flashSaleItemId,
                    quantity = 1
                }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadJsonAsync(response);
        Assert.Equal("Flash sale item is not active.", error.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_order_returns_bad_request_when_campaign_is_inactive()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var flashSaleItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 10,
            soldQuantity: 0,
            salePrice: 80m,
            campaignIsActive: false);

        var response = await PostOrderAsync(client, new
        {
            customerName = "Integration Customer",
            customerEmail = "customer@example.com",
            items = new[]
            {
                new
                {
                    flashSaleItemId,
                    quantity = 1
                }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadJsonAsync(response);
        Assert.Equal("Campaign is not active.", error.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_order_returns_bad_request_when_duplicate_lines_exceed_available_stock()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var flashSaleItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 5,
            soldQuantity: 0,
            salePrice: 80m);

        var response = await PostOrderAsync(client, new
        {
            customerName = "Integration Customer",
            customerEmail = "customer@example.com",
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
    }

    [Fact]
    public async Task Create_order_returns_bad_request_when_campaign_is_not_currently_running()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var flashSaleItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 10,
            soldQuantity: 0,
            salePrice: 80m,
            campaignStartsAt: DateTimeOffset.UtcNow.AddDays(-2),
            campaignEndsAt: DateTimeOffset.UtcNow.AddDays(-1));

        var response = await PostOrderAsync(client, new
        {
            customerName = "Integration Customer",
            customerEmail = "customer@example.com",
            items = new[]
            {
            new
            {
                flashSaleItemId,
                quantity = 1
            }
        }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadJsonAsync(response);
        Assert.Equal("Campaign is not currently running.", error.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_order_requires_idempotency_key()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var flashSaleItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 10,
            soldQuantity: 0,
            salePrice: 80m);

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            customerName = "Integration Customer",
            customerEmail = "customer@example.com",
            items = new[]
            {
            new
            {
                flashSaleItemId,
                quantity = 1
            }
        }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await ReadJsonAsync(response);
        Assert.Equal(
            "Idempotency-Key header is required.",
            error.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_order_with_same_idempotency_key_returns_existing_order_without_increasing_stock_again()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var flashSaleItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 10,
            soldQuantity: 0,
            salePrice: 80m);

        var idempotencyKey = Guid.NewGuid().ToString("N");

        var request = new
        {
            customerName = "Integration Customer",
            customerEmail = "customer@example.com",
            items = new[]
            {
            new
            {
                flashSaleItemId,
                quantity = 3
            }
        }
        };

        var firstResponse = await PostOrderAsync(client, request, idempotencyKey);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var firstOrder = await ReadJsonAsync(firstResponse);
        var firstOrderId = firstOrder.RootElement.GetProperty("id").GetGuid();

        var secondResponse = await PostOrderAsync(client, request, idempotencyKey);

        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var secondOrder = await ReadJsonAsync(secondResponse);
        Assert.Equal(firstOrderId, secondOrder.RootElement.GetProperty("id").GetGuid());

        var itemResponse = await client.GetAsync($"/api/flash-sale-items/{flashSaleItemId}");
        Assert.Equal(HttpStatusCode.OK, itemResponse.StatusCode);

        var item = await ReadJsonAsync(itemResponse);
        Assert.Equal(3, item.RootElement.GetProperty("soldQuantity").GetInt32());
        Assert.Equal(7, item.RootElement.GetProperty("availableQuantity").GetInt32());
    }

    [Fact]
    public async Task Create_order_returns_conflict_when_idempotency_key_is_reused_with_different_request()
    {
        await using var factory = new FlashSaleApiFactory();
        using var client = factory.CreateClient();

        var flashSaleItemId = await CreateFlashSaleItemAsync(
            client,
            totalQuantity: 10,
            soldQuantity: 0,
            salePrice: 80m);

        var idempotencyKey = Guid.NewGuid().ToString("N");

        var firstResponse = await PostOrderAsync(client, new
        {
            customerName = "Integration Customer",
            customerEmail = "customer@example.com",
            items = new[]
            {
            new
            {
                flashSaleItemId,
                quantity = 1
            }
        }
        }, idempotencyKey);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var secondResponse = await PostOrderAsync(client, new
        {
            customerName = "Integration Customer",
            customerEmail = "customer@example.com",
            items = new[]
            {
            new
            {
                flashSaleItemId,
                quantity = 2
            }
        }
        }, idempotencyKey);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var error = await ReadJsonAsync(secondResponse);
        Assert.Equal(
            "Idempotency-Key was already used with a different request.",
            error.RootElement.GetProperty("message").GetString());

        var itemResponse = await client.GetAsync($"/api/flash-sale-items/{flashSaleItemId}");
        Assert.Equal(HttpStatusCode.OK, itemResponse.StatusCode);

        var item = await ReadJsonAsync(itemResponse);
        Assert.Equal(1, item.RootElement.GetProperty("soldQuantity").GetInt32());
        Assert.Equal(9, item.RootElement.GetProperty("availableQuantity").GetInt32());
    }

    private static async Task<HttpResponseMessage> PostOrderAsync(HttpClient client, object request, string? idempotencyKey = null)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/orders");
        httpRequest.Headers.Add(
            "Idempotency-Key",
            idempotencyKey ?? Guid.NewGuid().ToString("N")
        );

        httpRequest.Content = JsonContent.Create(request);

        return await client.SendAsync(httpRequest);
    }

    private static async Task<Guid> CreateFlashSaleItemAsync(
        HttpClient client,
        int totalQuantity,
        int soldQuantity,
        decimal salePrice,
        DateTimeOffset? campaignStartsAt = null,
        DateTimeOffset? campaignEndsAt = null,
        bool isActive = true,
        bool campaignIsActive = true)
    {
        var productId = await CreateProductAsync(client);
        var campaignId = await CreateCampaignAsync(client, campaignStartsAt, campaignEndsAt, campaignIsActive);

        var response = await client.PostAsJsonAsync("/api/flash-sale-items", new
        {
            campaignId,
            productId,
            originalPrice = 100m,
            salePrice,
            totalQuantity,
            soldQuantity,
            isActive
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var item = await ReadJsonAsync(response);
        return item.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = $"Order Test Product {Guid.NewGuid()}",
            description = "Created by order integration test.",
            price = 100m,
            imageUrl = "https://example.com/order-product.png",
            isActive = true
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var product = await ReadJsonAsync(response);
        return product.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> CreateCampaignAsync(
    HttpClient client,
    DateTimeOffset? startsAt = null,
    DateTimeOffset? endsAt = null,
    bool isActive = true)
    {
        var response = await client.PostAsJsonAsync("/api/flash-sale-campaigns", new
        {
            name = $"Order Test Campaign {Guid.NewGuid()}",
            startsAt = startsAt ?? DateTimeOffset.UtcNow.AddHours(-1),
            endsAt = endsAt ?? DateTimeOffset.UtcNow.AddHours(1),
            isActive
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
