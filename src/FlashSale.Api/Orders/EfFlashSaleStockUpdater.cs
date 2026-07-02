using FlashSale.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashSale.Api.Orders;

public sealed class EfFlashSaleStockUpdater(FlashSaleDbContext dbContext) : IFlashSaleStockUpdater
{
    public async Task<bool> TryIncreaseSoldQuantityAsync(
        Guid flashSaleItemId,
        int quantity,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await dbContext.FlashSaleItems
            .Where(item =>
                item.Id == flashSaleItemId &&
                item.IsActive &&
                item.SoldQuantity + quantity <= item.TotalQuantity)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.SoldQuantity, item => item.SoldQuantity + quantity)
                .SetProperty(item => item.UpdatedAt, updatedAt),
                cancellationToken);

        return affectedRows == 1;
    }
}
