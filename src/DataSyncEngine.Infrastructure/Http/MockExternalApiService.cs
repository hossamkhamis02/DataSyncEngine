using DataSyncEngine.Contracts;
using DataSyncEngine.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataSyncEngine.Infrastructure.Http;

public class MockExternalApiService : IExternalApiClient
{
    private readonly ILogger<MockExternalApiService> _logger;
    private static readonly Lazy<IReadOnlyList<ExternalProductDto>> _allProducts =
        new(GenerateAllProducts);

    private static readonly string[] Categories = { "ELEC", "HOME", "SPRT", "TOOL", "FASH", "BOOK", "FOOD", "AUTO", "HEAL", "TOYS" };
    private static readonly string[] CategoryNames = { "Electronics", "Home & Garden", "Sports", "Tools", "Fashion", "Books", "Food", "Automotive", "Health", "Toys" };

    public MockExternalApiService(ILogger<MockExternalApiService> logger)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<ExternalProductDto>> FetchAllProductsAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Mock API: Returning {Count} deterministic products", _allProducts.Value.Count);
        return Task.FromResult(_allProducts.Value);
    }

    private static IReadOnlyList<ExternalProductDto> GenerateAllProducts()
    {
        var random = new Random(42);
        var products = new List<ExternalProductDto>(4500);

        for (int i = 0; i < 4500; i++)
        {
            var categoryIndex = random.Next(Categories.Length);
            var basePrice = Math.Round((decimal)(random.NextDouble() * 500 + 5), 2);

            products.Add(new ExternalProductDto
            {
                ExternalId = $"SKU-{(i + 1):D5}",
                Name = $"{CategoryNames[categoryIndex]} Product {i + 1}",
                CategoryCode = Categories[categoryIndex],
                Price = basePrice,
                StockQuantity = random.Next(0, 1001),
                IsActive = random.NextDouble() > 0.1,
                LastModifiedUtc = DateTime.UtcNow.AddDays(-random.Next(0, 30))
            });
        }

        return products.AsReadOnly();
    }
}
