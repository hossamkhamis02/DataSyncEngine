using DataSyncEngine.Contracts;

namespace DataSyncEngine.Core.Interfaces;

public interface IBulkRepository
{
    Task<int> StageProductsAsync(IReadOnlyList<ExternalProductDto> products, CancellationToken cancellationToken = default);
    Task<(int Inserted, int Updated)> MergeProductsAsync(CancellationToken cancellationToken = default);
    Task<int> SoftDeleteMissingAsync(CancellationToken cancellationToken = default);
    Task CleanupTempTableAsync(CancellationToken cancellationToken = default);
}
