
using AutoFixture;
using Simulation.Checkout.Domain;

namespace Simulation.Checkout.Tests.TestData;

public class OrderFixture
{
    private readonly Fixture _fixture = new();

    public Order CreateValidOrder()
    {
        var items = _fixture
            .Build<OrderItem>()
            .With(x => x.Quantity, 1)
            .With(x => x.UnitPrice, 100m)
            .With(x => x.ProductName, "Generated Product")
            .CreateMany(3)
            .ToList();

        return _fixture
            .Build<Order>()
            .With(x => x.CustomerName, "AutoFixture Customer")
            .With(x => x.CustomerEmail, "fixture@example.com")
            .With(x => x.Items, items)
            .Create();
    }
}
