using FlashSale.Api.Orders;
using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.IntegrationTests.Api;

public sealed class InMemoryFlashSaleStockUpdater(FlashSaleDbContext dbContext) : IFlashSaleStockUpdater
{
    public async Task<bool> TryIncreaseSoldQuantityAsync(
        Guid flashSaleItemId,
        int quantity,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.FlashSaleItems
            .FirstOrDefaultAsync(item => item.Id == flashSaleItemId, cancellationToken);

        if (item is null ||
            !item.IsActive ||
            item.SoldQuantity + quantity > item.TotalQuantity)
        {
            return false;
        }

        item.SoldQuantity += quantity;
        item.UpdatedAt = updatedAt;

        return true;
    }
}