namespace FlashSale.Domain.Entities;

public sealed class OrderIdempotencyKey
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public Order? Order { get; set; } = null;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
