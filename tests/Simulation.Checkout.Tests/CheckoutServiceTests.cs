
using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Domain.Exceptions;
using Simulation.Checkout.Tests.TestData;
namespace Simulation.Checkout.Tests;

public class CheckoutServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepository;
    private readonly Mock<IInventoryService> _inventoryService;
    private readonly Mock<INotificationService> _notificationService;

    private readonly CheckoutService _sut;

    public CheckoutServiceTests()
    {
        _orderRepository = new Mock<IOrderRepository>();
        _inventoryService = new Mock<IInventoryService>();
        _notificationService = new Mock<INotificationService>();

        _inventoryService
            .Setup(x => x.IsAvailableAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _sut = new CheckoutService(
            _orderRepository.Object,
            _inventoryService.Object,
            _notificationService.Object);
    }

    private static Order CreateOrder(
        decimal price = 100,
        int quantity = 1)
    {
        return new Order
        {
            CustomerName = "John",
            CustomerEmail = "john@example.com",
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Laptop",
                    UnitPrice = price,
                    Quantity = quantity
                }
            }
        };
    }

    // Tests will go here

    [Fact]
    public async Task CheckoutAsync_WhenOrderIsEmpty_ShouldThrowEmptyOrderException()
    {
        // Arrange
        var order = new Order
        {
            CustomerName = "John",
            CustomerEmail = "john@example.com",
            Items = new List<OrderItem>()
        };

        // Act
        var act = async () => await _sut.CheckoutAsync(order);

        // Assert
        await act.Should()
            .ThrowAsync<EmptyOrderException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CheckoutAsync_WhenQuantityIsInvalid_ShouldThrowInvalidQuantityException(
        int quantity)
    {
        // Arrange
        var order = CreateOrder(quantity: quantity);

        // Act
        var act = async () => await _sut.CheckoutAsync(order);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidQuantityException>()
            .WithMessage("*Laptop*");
    }

    [Fact]
    public async Task CheckoutAsync_WhenPriceIsNegative_ShouldThrowInvalidPriceException()
    {
        // Arrange
        var order = CreateOrder(price: -0.01m);

        // Act
        var act = async () => await _sut.CheckoutAsync(order);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidPriceException>()
            .WithMessage("*Laptop*");
    }

    [Fact]
    public async Task CheckoutAsync_WhenPriceIsZero_ShouldSucceed()
    {
        // Arrange
        var order = CreateOrder(price: 0m);

        // Act
        var receipt = await _sut.CheckoutAsync(order);

        // Assert
        receipt.Should().NotBeNull();
        receipt.Subtotal.Should().Be(0m);
        receipt.Discount.Should().Be(0m);
        receipt.Total.Should().Be(0m);
    }

    [Theory]
    [InlineData(999.99, 0)]
    [InlineData(1000, 100)]
    [InlineData(1500, 100)]
    public async Task CheckoutAsync_WhenSubtotalChanges_ShouldApplyCorrectDiscount(
        decimal subtotal,
        decimal expectedDiscount)
    {
        // Arrange
        var order = CreateOrder(price: subtotal, quantity: 1);

        // Act
        var receipt = await _sut.CheckoutAsync(order);

        // Assert
        receipt.Subtotal.Should().Be(subtotal);
        receipt.Discount.Should().Be(expectedDiscount);
        receipt.Total.Should().Be(subtotal - expectedDiscount);
    }

    [Fact]
    public async Task CheckoutAsync_WhenUsingExamplesAAndC_ShouldCalculateCorrectAmounts()
    {
        // Arrange
        var orderA = new Order
        {
            CustomerName = "John",
            CustomerEmail = "john@example.com",
            Items = new List<OrderItem>
        {
            new()
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product A",
                UnitPrice = 150m,
                Quantity = 2
            },
            new()
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Product B",
                UnitPrice = 400m,
                Quantity = 1
            }
        }
        };

        var orderC = CreateOrder(price: 500m, quantity: 3);

        // Act
        var receiptA = await _sut.CheckoutAsync(orderA);
        var receiptC = await _sut.CheckoutAsync(orderC);

        // Assert
        receiptA.Subtotal.Should().Be(700m);
        receiptA.Discount.Should().Be(0m);
        receiptA.Total.Should().Be(700m);

        receiptC.Subtotal.Should().Be(1500m);
        receiptC.Discount.Should().Be(100m);
        receiptC.Total.Should().Be(1400m);
    }

    [Fact]
public async Task CheckoutAsync_WhenOrderIsValid_ShouldSaveOrderAndSendConfirmationOnce()
{
    // Arrange
    var order = CreateOrder(price: 500m, quantity: 2);

    // Act
    var receipt = await _sut.CheckoutAsync(order);

    // Assert
    receipt.Should().NotBeNull();

    _orderRepository.Verify(
        x => x.SaveAsync(order, It.IsAny<CancellationToken>()),
        Times.Once);

    _notificationService.Verify(
        x => x.SendOrderConfirmationAsync(
            It.Is<CheckoutReceipt>(r => r.OrderId == order.Id),
            It.IsAny<CancellationToken>()),
        Times.Once);
}

[Fact]
public async Task CheckoutAsync_WhenItemIsOutOfStock_ShouldNotSaveOrNotify()
{
    // Arrange
    var order = CreateOrder();

    _inventoryService
        .Setup(x => x.IsAvailableAsync(
            It.IsAny<Guid>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(false);

    // Act
    var act = async () => await _sut.CheckoutAsync(order);

    // Assert
    await act.Should()
        .ThrowAsync<InsufficientInventoryException>();

    _orderRepository.Verify(
        x => x.SaveAsync(
            It.IsAny<Order>(),
            It.IsAny<CancellationToken>()),
        Times.Never);

    _notificationService.Verify(
        x => x.SendOrderConfirmationAsync(
            It.IsAny<CheckoutReceipt>(),
            It.IsAny<CancellationToken>()),
        Times.Never);
}

[Fact]
public async Task CheckoutAsync_WhenOrderValidationFails_ShouldNotCallDependencies()
{
    // Arrange
    var order = CreateOrder(quantity: 0);

    // Act
    var act = async () => await _sut.CheckoutAsync(order);

    // Assert
    await act.Should()
        .ThrowAsync<InvalidQuantityException>();

    _inventoryService.Verify(
        x => x.IsAvailableAsync(
            It.IsAny<Guid>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()),
        Times.Never);

    _orderRepository.Verify(
        x => x.SaveAsync(
            It.IsAny<Order>(),
            It.IsAny<CancellationToken>()),
        Times.Never);

    _notificationService.Verify(
        x => x.SendOrderConfirmationAsync(
            It.IsAny<CheckoutReceipt>(),
            It.IsAny<CancellationToken>()),
        Times.Never);
}





[Fact]
public async Task CheckoutAsync_WhenOrderIsCreatedByAutoFixture_ShouldSucceed()
{
    // Arrange
    var orderFixture = new OrderFixture();
    var order = orderFixture.CreateValidOrder();

    // Act
    var receipt = await _sut.CheckoutAsync(order);

    // Assert
    receipt.Should().NotBeNull();
    receipt.Items.Should().HaveCount(3);
    receipt.Items.Should().OnlyContain(x => x.Quantity > 0);
    receipt.Items.Should().OnlyContain(x => x.UnitPrice >= 0);
}


[Fact]
public async Task CheckoutAsync_WhenCustomerIsGeneratedByBogus_ShouldPreserveCustomerData()
{
    // Arrange
    var customerFaker = new CustomerFaker();
    var customer = customerFaker.Generate();

    var order = new Order
    {
        CustomerName = customer.Name,
        CustomerEmail = customer.Email,
        Items = new List<OrderItem>
        {
            new()
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Laptop",
                UnitPrice = 500m,
                Quantity = 1
            }
        }
    };

    // Act
    var receipt = await _sut.CheckoutAsync(order);

    // Assert
    receipt.CustomerName.Should().Be(customer.Name);
    receipt.CustomerEmail.Should().Be(customer.Email);
}


}