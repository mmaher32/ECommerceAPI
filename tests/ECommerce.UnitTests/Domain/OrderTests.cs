using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Errors;

namespace ECommerce.UnitTests.Domain;

public class OrderTests
{
    private static ShippingAddress Address() =>
        ShippingAddress.Create("Ada", "Lovelace", "0100", "EG", "Cairo", "Main St", "11511").Value;

    private static OrderItem Item(decimal price = 10m, int quantity = 2) =>
        OrderItem.Create(Guid.NewGuid(), "Boots", "/img.png", price, quantity).Value;

    private static Order NewOrder(params OrderItem[] items) =>
        Order.Create(Guid.NewGuid(), "a@b.com", Address(), "Standard", 5m,
            items.Length == 0 ? [Item()] : items).Value;

    [Fact]
    public void Create_ComputesSubtotalAndTotal()
    {
        var order = NewOrder(Item(10m, 2), Item(3.5m, 1));

        Assert.Equal(23.5m, order.Subtotal);
        Assert.Equal(28.5m, order.Total);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void Create_WithNoItems_Fails()
    {
        var result = Order.Create(Guid.NewGuid(), "a@b.com", Address(), "Standard", 5m, []);

        Assert.Equal(OrderErrors.EmptyOrder, result.Error);
    }

    [Fact]
    public void Create_WithDuplicateProduct_Fails()
    {
        var productId = Guid.NewGuid();
        var a = OrderItem.Create(productId, "Boots", "/img.png", 10m, 1).Value;
        var b = OrderItem.Create(productId, "Boots", "/img.png", 10m, 2).Value;

        var result = Order.Create(Guid.NewGuid(), "a@b.com", Address(), "Standard", 5m, [a, b]);

        Assert.Equal(OrderErrors.DuplicateOrderItem, result.Error);
    }

    [Fact]
    public void Create_WithNegativeDeliveryPrice_Fails()
    {
        var result = Order.Create(Guid.NewGuid(), "a@b.com", Address(), "Standard", -1m, [Item()]);

        Assert.Equal(OrderErrors.InvalidDeliveryPrice, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(OrderItem.MaxQuantity + 1)]
    public void OrderItem_WithQuantityOutOfRange_Fails(int quantity)
    {
        var result = OrderItem.Create(Guid.NewGuid(), "Boots", "/img.png", 10m, quantity);

        Assert.Equal(OrderErrors.InvalidQuantity, result.Error);
    }

    [Fact]
    public void MarkAsPaid_WithoutPaymentIntent_Fails()
    {
        var order = NewOrder();

        var result = order.MarkAsPaid();

        Assert.Equal(OrderErrors.PaymentIntentRequired, result.Error);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void FullLifecycle_PendingToDelivered_Succeeds()
    {
        var order = NewOrder();

        Assert.True(order.SetPaymentIntent("pi_123").IsSuccess);
        Assert.True(order.MarkAsPaid().IsSuccess);
        Assert.True(order.MarkAsPaid().IsSuccess); // webhooks repeat; idempotent
        Assert.True(order.MarkAsShipped().IsSuccess);
        Assert.True(order.MarkAsDelivered().IsSuccess);

        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void SetPaymentIntent_Twice_WithDifferentId_Fails()
    {
        var order = NewOrder();
        order.SetPaymentIntent("pi_1");

        var result = order.SetPaymentIntent("pi_2");

        Assert.Equal(OrderErrors.PaymentIntentAlreadySet, result.Error);
    }

    [Fact]
    public void Cancel_AfterShipping_Fails()
    {
        var order = NewOrder();
        order.SetPaymentIntent("pi_1");
        order.MarkAsPaid();
        order.MarkAsShipped();

        var result = order.Cancel();

        Assert.True(result.IsFailure);
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void MarkAsShipped_BeforePayment_Fails()
    {
        var order = NewOrder();

        Assert.True(order.MarkAsShipped().IsFailure);
    }
}
