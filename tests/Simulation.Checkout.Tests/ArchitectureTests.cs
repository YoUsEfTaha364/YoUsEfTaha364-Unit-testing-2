
using FluentAssertions;
using NetArchTest.Rules;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Application;
using Simulation.Checkout.Infrastructure;
using Simulation.Checkout.Application.Abstractions;

namespace Simulation.Checkout.Tests;

public class ArchitectureTests
{
    [Fact]
    public void Domain_ShouldNotDependOnApplicationOrInfrastructure()
    {
        // Arrange
        var domainAssembly = typeof(Order).Assembly;

        // Act
        var result = Types.InAssembly(domainAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Simulation.Checkout.Application",
                "Simulation.Checkout.Infrastructure")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }


    [Fact]
    public void Application_ShouldNotDependOnInfrastructure()
    {
        // Arrange
        var applicationAssembly = typeof(CheckoutService).Assembly;

        // Act
        var result = Types.InAssembly(applicationAssembly)
            .Should()
            .NotHaveDependencyOn("Simulation.Checkout.Infrastructure")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void AllInterfaces_ShouldStartWithI()
    {
        // Arrange
        var assemblies = new[]
        {
        typeof(Order).Assembly,
        typeof(CheckoutService).Assembly,
        typeof(InMemoryOrderRepository).Assembly
    };

        // Act
        var results = assemblies.Select(assembly =>
            Types.InAssembly(assembly)
                .That()
                .AreInterfaces()
                .Should()
                .HaveNameStartingWith("I")
                .GetResult());

        // Assert
        results.Should().OnlyContain(result => result.IsSuccessful);
    }



    [Fact]
    public void ClassesImplementingIOrderRepository_ShouldEndWithRepository()
    {
        // Arrange
        var infrastructureAssembly =
            typeof(InMemoryOrderRepository).Assembly;

        // Act
        var result = Types.InAssembly(infrastructureAssembly)
            .That()
            .ImplementInterface(typeof(IOrderRepository))
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue();
    }


}
