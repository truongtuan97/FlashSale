namespace FlashSale.Api.Orders;

public interface IFlashSaleStockUpdater
{
    Task<bool> TryIncreaseSoldQuantityAsync(
        Guid flashSaleItemId,
        int quantity,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default);
}