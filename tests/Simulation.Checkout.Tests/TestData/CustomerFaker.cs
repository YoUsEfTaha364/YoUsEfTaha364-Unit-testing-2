
using Bogus;

namespace Simulation.Checkout.Tests.TestData;

public record CustomerData(
    string Name,
    string Email);

public class CustomerFaker
{
    private readonly Faker<CustomerData> _faker;

    public CustomerFaker()
    {
        _faker = new Faker<CustomerData>()
            .UseSeed(12345)
            .CustomInstantiator(faker =>
                new CustomerData(
                    faker.Name.FullName(),
                    faker.Internet.Email()));
    }

    public CustomerData Generate()
    {
        return _faker.Generate();
    }
}
