namespace FlashSale.Domain.Entities;
public sealed class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid FlashSaleItemId { get; set; }
    public FlashSaleItem FlashSaleItem { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}