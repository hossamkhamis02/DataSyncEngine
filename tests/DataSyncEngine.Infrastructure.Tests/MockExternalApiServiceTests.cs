using DataSyncEngine.Infrastructure.Http;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DataSyncEngine.Infrastructure.Tests;

public class MockExternalApiServiceTests
{
    [Fact]
    public async Task FetchAllProducts_ReturnsExactly4500Records()
    {
        var service = new MockExternalApiService(
            new LoggerFactory().CreateLogger<MockExternalApiService>());

        var products = await service.FetchAllProductsAsync();

        products.Should().HaveCount(4500);
    }

    [Fact]
    public async Task FetchAllProducts_IsDeterministic()
    {
        var service1 = new MockExternalApiService(
            new LoggerFactory().CreateLogger<MockExternalApiService>());
        var service2 = new MockExternalApiService(
            new LoggerFactory().CreateLogger<MockExternalApiService>());

        var result1 = await service1.FetchAllProductsAsync();
        var result2 = await service2.FetchAllProductsAsync();

        result1.Should().BeEquivalentTo(result2);
    }

    [Fact]
    public async Task FetchAllProducts_AllRecordsHaveValidData()
    {
        var service = new MockExternalApiService(
            new LoggerFactory().CreateLogger<MockExternalApiService>());

        var products = await service.FetchAllProductsAsync();

        products.Should().AllSatisfy(p =>
        {
            p.ExternalId.Should().NotBeNullOrEmpty();
            p.Name.Should().NotBeNullOrEmpty();
            p.CategoryCode.Should().NotBeNullOrEmpty();
            p.Price.Should().BeGreaterThan(0);
            p.StockQuantity.Should().BeGreaterOrEqualTo(0);
        });
    }
}
