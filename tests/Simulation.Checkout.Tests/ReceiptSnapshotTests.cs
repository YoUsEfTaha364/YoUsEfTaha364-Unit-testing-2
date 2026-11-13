
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Moq;
using VerifyTests;

namespace Simulation.Checkout.Tests;

public class ReceiptSnapshotTests
{
    [Fact]
    public async Task CheckoutAsync_WhenUsingExampleC_ShouldMatchReceiptSnapshot()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CustomerName = "John",
            CustomerEmail = "john@example.com",
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    ProductName = "Laptop",
                    UnitPrice = 500m,
                    Quantity = 3
                }
            }
        };

        var orderRepository = new Mock<IOrderRepository>();
        var inventoryService = new Mock<IInventoryService>();
        var notificationService = new Mock<INotificationService>();

        inventoryService
            .Setup(x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new CheckoutService(
            orderRepository.Object,
            inventoryService.Object,
            notificationService.Object);

        // Act
        var receipt = await sut.CheckoutAsync(order);

        // Assert
        var settings = new VerifySettings();

        settings.IgnoreMember<CheckoutReceipt>(x => x.OrderId);
        settings.IgnoreMember<CheckoutReceipt>(x => x.CreatedAt);

        await Verifier.Verify(receipt, settings);
    }
}
