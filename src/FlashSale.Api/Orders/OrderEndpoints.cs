using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlashSale.Domain.Entities;
using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Api.Orders;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this WebApplication app)
    {
        var orders = app.MapGroup("/api/orders").WithTags("Orders");

        orders.MapGet("/{id:guid}", async (Guid id, FlashSaleDbContext dbContext) =>
        {
            var order = await IncludeOrderResponseDetails(dbContext.Orders.AsNoTracking())
                .FirstOrDefaultAsync(order => order.Id == id);

            return order is null ? Results.NotFound() : Results.Ok(OrderResponse.FromOrder(order));
        }).WithName("GetOrderById");

        orders.MapPost("/", async (
            CreateOrderRequest request, 
            HttpRequest httpRequest,
            FlashSaleDbContext dbContext, 
            IFlashSaleStockUpdater stockUpdater) =>
        {
            if (!httpRequest.Headers.TryGetValue("Idempotency-Key", out var idempotencyKeyValues) || string.IsNullOrWhiteSpace(idempotencyKeyValues.FirstOrDefault()))
            {
                return Results.BadRequest(new { message = "Idempotency-Key header is required."} );
            }

            var idempotencyKey = idempotencyKeyValues.First()!.Trim();

            if (idempotencyKey.Length > 200)
            {
                return Results.BadRequest(new { message = "Idempotency-Key must be 200 characters or fewer." });
            }

            var requestHash = OrderRequestHasher.Hash(request);

            var validationError = OrderRequestValidator.Validate(request);
            if (validationError != null)
            {
                return Results.BadRequest(new { message = validationError });
            }

            var existingIdempotencyKey = await dbContext.OrderIdempotencyKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(key => key.Key == idempotencyKey);

            if (existingIdempotencyKey is not null)
            {
                if (existingIdempotencyKey.RequestHash != requestHash)
                {
                    return Results.Conflict(new { message = "Idempotency-Key was already used with a different request." });
                }

                var existingOrder = await IncludeOrderResponseDetails(dbContext.Orders.AsNoTracking())
                    .FirstAsync(order => order.Id == existingIdempotencyKey.OrderId);

                return Results.Ok(OrderResponse.FromOrder(existingOrder));
            }

            var requestItemsIds = request.Items.Select(item => item.FlashSaleItemId).Distinct().ToArray();

            var flashSaleItems = await dbContext.FlashSaleItems
                .Include(item => item.Campaign)
                .Include(item => item.Product)
                .Where(item => requestItemsIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id);

            var now = DateTimeOffset.UtcNow;

            var requestedQuantityByItemId = request.Items
                .GroupBy(item => item.FlashSaleItemId)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

            foreach (var itemRequest in requestedQuantityByItemId)
            {
                if (!flashSaleItems.TryGetValue(itemRequest.Key, out var flashSaleItem))
                {
                    return Results.BadRequest(new { message = $"Flash sale item does not exist." });
                }

                if (!flashSaleItem.IsActive)
                {
                    return Results.BadRequest(new { message = $"Flash sale item is not active." });
                }

                if (!flashSaleItem.Campaign.IsActive)
                {
                    return Results.BadRequest(new { message = $"Campaign is not active." });
                }

                if (now < flashSaleItem.Campaign.StartsAt || now > flashSaleItem.Campaign.EndsAt)
                {
                    return Results.BadRequest(new { message = $"Campaign is not currently running." });
                }

                if (flashSaleItem.SoldQuantity + itemRequest.Value > flashSaleItem.TotalQuantity)
                {
                    return Results.BadRequest(new { message = $"Not enough stock." });
                }
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            foreach (var itemRequest in requestedQuantityByItemId)
            {
                var stockUpdated = await stockUpdater.TryIncreaseSoldQuantityAsync(
                    itemRequest.Key,
                    itemRequest.Value,
                    now);

                if (!stockUpdated)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = "Not enough stock." });
                }
            }

            var order = new Domain.Entities.Order
            {
                Id = Guid.NewGuid(),
                CustomerName = request.CustomerName.Trim(),
                CustomerEmail = request.CustomerEmail.Trim(),
                Status = Domain.Entities.OrderStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now,
            };

            foreach (var requestItem in request.Items)
            {
                var flashSaleItem = flashSaleItems[requestItem.FlashSaleItemId];

                var orderItem = new Domain.Entities.OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    FlashSaleItemId = flashSaleItem.Id,
                    FlashSaleItem = flashSaleItem,
                    Quantity = requestItem.Quantity,
                    UnitPrice = flashSaleItem.SalePrice,
                    LineTotal = flashSaleItem.SalePrice * requestItem.Quantity,
                    CreatedAt = now,
                };

                order.Items.Add(orderItem);
            }

            order.TotalAmount = order.Items.Sum(item => item.LineTotal);

            dbContext.Orders.Add(order);

            dbContext.OrderIdempotencyKeys.Add(new OrderIdempotencyKey
            {
                Id = Guid.NewGuid(),
                Key = idempotencyKey, 
                RequestHash = requestHash,
                OrderId = order.Id,
                Order = order,
                CreatedAt = now,
                UpdatedAt = now
            });

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var response = OrderResponse.FromOrder(order);
            return Results.CreatedAtRoute("GetOrderById", new { id = order.Id }, response);
        }).WithName("CreateOrder");

        return orders;
    }

    private static IQueryable<Domain.Entities.Order> IncludeOrderResponseDetails(
        IQueryable<Domain.Entities.Order> query)
    {
        return query
            .Include(order => order.Items)
            .ThenInclude(item => item.FlashSaleItem)
            .ThenInclude(item => item.Product)
            .Include(order => order.Items)
            .ThenInclude(item => item.FlashSaleItem)
            .ThenInclude(item => item.Campaign);
    }
}

public sealed record CreateOrderRequest(
    string CustomerName,
    string CustomerEmail,
    IReadOnlyList<CreateOrderItemRequest> Items
);

public sealed record CreateOrderItemRequest(
    Guid FlashSaleItemId,
    int Quantity
);

public static class OrderRequestValidator
{
    public static string? Validate(CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            return "Customer name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            return "Customer email is required.";
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            return "Order must contain at least one item.";
        }

        foreach (var item in request.Items)
        {
            if (item.FlashSaleItemId == Guid.Empty)
            {
                return "Flash sale item is required.";
            }

            if (item.Quantity <= 0)
            {
                return "Quantity must be greater than 0.";
            }
        }

        return null; // No validation errors
    }
}

public static class OrderRequestHasher
{
    private static readonly JsonSerializerOptions JsonOptions = new (JsonSerializerDefaults.Web);
    public static string Hash(CreateOrderRequest request)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes);
    }
}

public sealed record OrderResponse(
    Guid Id,
    string CustomerName,
    string CustomerEmail,
    Domain.Entities.OrderStatus Status,
    decimal TotalAmount,
    IReadOnlyList<OrderItemResponse> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static OrderResponse FromOrder(Domain.Entities.Order order)
    {
        return new OrderResponse(
            order.Id,
            order.CustomerName,
            order.CustomerEmail,
            order.Status,
            order.TotalAmount,
            order.Items
                .OrderBy(item => item.CreatedAt)
                .Select(OrderItemResponse.FromOrderItem)
                .ToArray(),
            order.CreatedAt,
            order.UpdatedAt);
    }
}

public sealed record OrderItemResponse(
    Guid FlashSaleItemId,
    string ProductName,
    string CampaignName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal)
{
    public static OrderItemResponse FromOrderItem(Domain.Entities.OrderItem item)
    {
        return new OrderItemResponse(
            item.FlashSaleItemId,
            item.FlashSaleItem.Product.Name,
            item.FlashSaleItem.Campaign.Name,
            item.Quantity,
            item.UnitPrice,
            item.LineTotal);
    }
}
