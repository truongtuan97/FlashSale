namespace FlashSale.Domain.Entities;

public sealed class FlashSaleItem
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public FlashSaleCampaign Campaign { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal OriginalPrice { get; set; }
    public decimal SalePrice { get; set; }
    public int TotalQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<OrderItem> OrderItems { get; set; } = [];
}
