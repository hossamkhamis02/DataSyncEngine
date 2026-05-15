using DataSyncEngine.Contracts;
using DataSyncEngine.Core.Interfaces;
using DataSyncEngine.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataSyncEngine.Infrastructure.Tests;

public class SqlServerBulkRepositoryTests
{
    [Fact]
    public void Constructor_WithValidConnectionString_CreatesInstance()
    {
        var repo = new SqlServerBulkRepository(
            "Server=.;Database=Test;Trusted_Connection=True;TrustServerCertificate=True;",
            Mock.Of<ILogger<SqlServerBulkRepository>>());

        repo.Should().NotBeNull();
    }

    [Fact]
    public async Task StageProductsAsync_BuildsCorrectDataTable()
    {
        var products = new List<ExternalProductDto>
        {
            new() { ExternalId = "SKU-001", Name = "Product 1", CategoryCode = "ELEC", Price = 99.99m, StockQuantity = 10, IsActive = true, LastModifiedUtc = DateTime.UtcNow },
            new() { ExternalId = "SKU-002", Name = "Product 2", CategoryCode = "HOME", Price = 49.99m, StockQuantity = 0, IsActive = false, LastModifiedUtc = DateTime.UtcNow }
        };

        var repo = new SqlServerBulkRepository(
            "Server=.\\SQLEXPRESS;Database=Test;Trusted_Connection=True;TrustServerCertificate=True;",
            Mock.Of<ILogger<SqlServerBulkRepository>>());

        var action = () => repo.StageProductsAsync(products);

        await action.Should().ThrowAsync<SqlException>();
    }
}
