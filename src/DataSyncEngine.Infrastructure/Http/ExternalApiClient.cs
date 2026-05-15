using DataSyncEngine.Contracts;
using DataSyncEngine.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace DataSyncEngine.Infrastructure.Http;

public class ExternalApiClient : IExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiClient> _logger;

    public ExternalApiClient(HttpClient httpClient, ILogger<ExternalApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExternalProductDto>> FetchAllProductsAsync(
        CancellationToken cancellationToken = default)
    {
        var allProducts = new List<ExternalProductDto>();
        var page = 1;
        var totalFetched = 0;
        var totalCount = 0;
        var firstPage = true;

        do
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            _logger.LogInformation("Fetching page {Page}", page);

            var response = await _httpClient.GetAsync(
                $"?page={page}&pageSize=100", cancellationToken);

            response.EnsureSuccessStatusCode();

            var paginatedResult = await response.Content
                .ReadFromJsonAsync<PaginatedResponse<ExternalProductDto>>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    cancellationToken);

            if (paginatedResult is null)
                break;

            if (firstPage)
            {
                totalCount = paginatedResult.TotalCount;
                _logger.LogInformation(
                    "Total records to fetch: {TotalCount}, Pages: ~{Pages}",
                    totalCount,
                    (int)Math.Ceiling(totalCount / 100.0));
                firstPage = false;
            }

            if (paginatedResult.Items.Count > 0)
            {
                allProducts.AddRange(paginatedResult.Items);
                totalFetched += paginatedResult.Items.Count;
            }

            page++;

            if (totalFetched >= totalCount)
                break;

        } while (true);

        _logger.LogInformation(
            "Fetched {Count} products across {Pages} pages",
            allProducts.Count, page - 1);

        return allProducts.AsReadOnly();
    }
}
