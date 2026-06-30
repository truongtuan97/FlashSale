using FlashSale.Domain.Entities;
using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Api.FlashSaleItems;

public static class FlashSaleItemEndpoints
{
    public static RouteGroupBuilder MapFlashSaleItemEndpoints(this WebApplication app)
    {
        var flashSaleItems = app.MapGroup("/api/flash-sale-items").WithTags("Flash Sale Items");

        flashSaleItems.MapGet("/", async (FlashSaleDbContext dbContext) =>
        {
            var items = await dbContext.FlashSaleItems
                .AsNoTracking()
                .Include(item => item.Campaign)
                .Include(item => item.Product)
                .OrderBy(item => item.Campaign.StartsAt)
                .ThenBy(item => item.Product.Name)
                .Select(item => FlashSaleItemResponse.FromFlashSaleItem(item))
                .ToArrayAsync();

            return Results.Ok(items);
        })
        .WithName("GetFlashSaleItems");

        flashSaleItems.MapGet("/{id:guid}", async (Guid id, FlashSaleDbContext dbContext) =>
        {
            var item = await dbContext.FlashSaleItems
                .AsNoTracking()
                .Include(item => item.Campaign)
                .Include(item => item.Product)
                .Where(item => item.Id == id)
                .Select(item => FlashSaleItemResponse.FromFlashSaleItem(item))
                .FirstOrDefaultAsync();

            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName("GetFlashSaleItemById");

        flashSaleItems.MapPost("/", async (CreateFlashSaleItemRequest request, FlashSaleDbContext dbContext) =>
        {
            var validationError = FlashSaleItemValidator.Validate(
                request.CampaignId,
                request.ProductId,
                request.OriginalPrice,
                request.SalePrice,
                request.TotalQuantity,
                request.SoldQuantity);

            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }

            var campaignExists = await dbContext.FlashSaleCampaigns.AnyAsync(campaign => campaign.Id == request.CampaignId);
            if (!campaignExists)
            {
                return Results.BadRequest(new { message = "Campaign does not exist." });
            }

            var productExists = await dbContext.Products.AnyAsync(product => product.Id == request.ProductId);
            if (!productExists)
            {
                return Results.BadRequest(new { message = "Product does not exist." });
            }

            var itemExists = await dbContext.FlashSaleItems.AnyAsync(item =>
                item.CampaignId == request.CampaignId && item.ProductId == request.ProductId);
            if (itemExists)
            {
                return Results.Conflict(new { message = "Product already exists in this campaign." });
            }

            var now = DateTimeOffset.UtcNow;
            var item = new FlashSaleItem
            {
                Id = Guid.NewGuid(),
                CampaignId = request.CampaignId,
                ProductId = request.ProductId,
                OriginalPrice = request.OriginalPrice,
                SalePrice = request.SalePrice,
                TotalQuantity = request.TotalQuantity,
                SoldQuantity = request.SoldQuantity,
                IsActive = request.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.FlashSaleItems.Add(item);
            await dbContext.SaveChangesAsync();

            var response = await dbContext.FlashSaleItems
                .AsNoTracking()
                .Include(savedItem => savedItem.Campaign)
                .Include(savedItem => savedItem.Product)
                .Where(savedItem => savedItem.Id == item.Id)
                .Select(savedItem => FlashSaleItemResponse.FromFlashSaleItem(savedItem))
                .FirstAsync();

            return Results.CreatedAtRoute("GetFlashSaleItemById", new { id = item.Id }, response);
        })
        .WithName("CreateFlashSaleItem");

        flashSaleItems.MapPut("/{id:guid}", async (Guid id, UpdateFlashSaleItemRequest request, FlashSaleDbContext dbContext) =>
        {
            var validationError = FlashSaleItemValidator.Validate(
                request.CampaignId,
                request.ProductId,
                request.OriginalPrice,
                request.SalePrice,
                request.TotalQuantity,
                request.SoldQuantity);

            if (validationError is not null)
            {
                return Results.BadRequest(new { message = validationError });
            }

            var item = await dbContext.FlashSaleItems.FindAsync(id);
            if (item is null)
            {
                return Results.NotFound();
            }

            var campaignExists = await dbContext.FlashSaleCampaigns.AnyAsync(campaign => campaign.Id == request.CampaignId);
            if (!campaignExists)
            {
                return Results.BadRequest(new { message = "Campaign does not exist." });
            }

            var productExists = await dbContext.Products.AnyAsync(product => product.Id == request.ProductId);
            if (!productExists)
            {
                return Results.BadRequest(new { message = "Product does not exist." });
            }

            var duplicateExists = await dbContext.FlashSaleItems.AnyAsync(existingItem =>
                existingItem.Id != id &&
                existingItem.CampaignId == request.CampaignId &&
                existingItem.ProductId == request.ProductId);
            if (duplicateExists)
            {
                return Results.Conflict(new { message = "Product already exists in this campaign." });
            }

            item.CampaignId = request.CampaignId;
            item.ProductId = request.ProductId;
            item.OriginalPrice = request.OriginalPrice;
            item.SalePrice = request.SalePrice;
            item.TotalQuantity = request.TotalQuantity;
            item.SoldQuantity = request.SoldQuantity;
            item.IsActive = request.IsActive;
            item.UpdatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync();

            var response = await dbContext.FlashSaleItems
                .AsNoTracking()
                .Include(updatedItem => updatedItem.Campaign)
                .Include(updatedItem => updatedItem.Product)
                .Where(updatedItem => updatedItem.Id == item.Id)
                .Select(updatedItem => FlashSaleItemResponse.FromFlashSaleItem(updatedItem))
                .FirstAsync();

            return Results.Ok(response);
        })
        .WithName("UpdateFlashSaleItem");

        flashSaleItems.MapDelete("/{id:guid}", async (Guid id, FlashSaleDbContext dbContext) =>
        {
            var item = await dbContext.FlashSaleItems.FindAsync(id);
            if (item is null)
            {
                return Results.NotFound();
            }

            dbContext.FlashSaleItems.Remove(item);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteFlashSaleItem");

        return flashSaleItems;
    }
}

public static class FlashSaleItemValidator
{
    public static string? Validate(
        Guid campaignId,
        Guid productId,
        decimal originalPrice,
        decimal salePrice,
        int totalQuantity,
        int soldQuantity)
    {
        if (campaignId == Guid.Empty)
        {
            return "Campaign is required.";
        }

        if (productId == Guid.Empty)
        {
            return "Product is required.";
        }

        if (originalPrice <= 0)
        {
            return "Original price must be greater than 0.";
        }

        if (salePrice <= 0)
        {
            return "Sale price must be greater than 0.";
        }

        if (salePrice > originalPrice)
        {
            return "Sale price must be less than or equal to original price.";
        }

        if (totalQuantity <= 0)
        {
            return "Total quantity must be greater than 0.";
        }

        if (soldQuantity < 0)
        {
            return "Sold quantity cannot be negative.";
        }

        if (soldQuantity > totalQuantity)
        {
            return "Sold quantity cannot be greater than total quantity.";
        }

        return null;
    }
}

public sealed record CreateFlashSaleItemRequest(
    Guid CampaignId,
    Guid ProductId,
    decimal OriginalPrice,
    decimal SalePrice,
    int TotalQuantity,
    int SoldQuantity = 0,
    bool IsActive = true);

public sealed record UpdateFlashSaleItemRequest(
    Guid CampaignId,
    Guid ProductId,
    decimal OriginalPrice,
    decimal SalePrice,
    int TotalQuantity,
    int SoldQuantity,
    bool IsActive);

public sealed record FlashSaleItemResponse(
    Guid Id,
    Guid CampaignId,
    string CampaignName,
    Guid ProductId,
    string ProductName,
    decimal OriginalPrice,
    decimal SalePrice,
    int TotalQuantity,
    int SoldQuantity,
    int AvailableQuantity,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static FlashSaleItemResponse FromFlashSaleItem(FlashSaleItem item)
    {
        return new FlashSaleItemResponse(
            item.Id,
            item.CampaignId,
            item.Campaign.Name,
            item.ProductId,
            item.Product.Name,
            item.OriginalPrice,
            item.SalePrice,
            item.TotalQuantity,
            item.SoldQuantity,
            item.TotalQuantity - item.SoldQuantity,
            item.IsActive,
            item.CreatedAt,
            item.UpdatedAt);
    }
}
