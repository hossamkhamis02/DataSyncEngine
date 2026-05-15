using DataSyncEngine.Contracts;

namespace DataSyncEngine.Core.Interfaces;

public interface IExternalApiClient
{
    Task<IReadOnlyList<ExternalProductDto>> FetchAllProductsAsync(CancellationToken cancellationToken = default);
}
