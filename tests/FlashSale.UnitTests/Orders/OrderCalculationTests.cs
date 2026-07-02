using FlashSale.Domain.Entities;

namespace FlashSale.UnitTests.Orders;

public sealed class OrderCalculationTests
{
    [Fact]
    public void Order_total_amount_should_equal_sum_of_order_item_line_totals()
    {
        var order = new Order
        {
            Items = [
                new OrderItem { Quantity = 2, UnitPrice = 100m, LineTotal = 200m },
                new OrderItem { Quantity = 1, UnitPrice = 50m, LineTotal = 50.00m }
            ]            
        };
        order.TotalAmount = order.Items.Sum(i => i.LineTotal);
        Assert.Equal(250m, order.TotalAmount);
    }

    [Fact]
    public void Order_item_line_total_should_equal_quantity_times_unit_price()
    {
        var orderItem = new OrderItem
        {
            Quantity = 3,
            UnitPrice = 99.99m
        };
        orderItem.LineTotal = orderItem.Quantity * orderItem.UnitPrice;
        Assert.Equal(299.97m, orderItem.LineTotal);
    }
}