using DataSyncEngine.Contracts;
using DataSyncEngine.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataSyncEngine.Core.Services;

public class SyncOrchestrator : ISyncOrchestrator
{
    private readonly IExternalApiClient _apiClient;
    private readonly IBulkRepository _bulkRepository;
    private readonly ISyncLogger _syncLogger;
    private readonly ILogger<SyncOrchestrator> _logger;

    public SyncOrchestrator(
        IExternalApiClient apiClient,
        IBulkRepository bulkRepository,
        ISyncLogger syncLogger,
        ILogger<SyncOrchestrator> logger)
    {
        _apiClient = apiClient;
        _bulkRepository = bulkRepository;
        _syncLogger = syncLogger;
        _logger = logger;
    }

    public async Task<SyncResult> RunSyncAsync(
        string jobName,
        CancellationToken cancellationToken = default)
    {
        var result = new SyncResult
        {
            JobName = jobName,
            StartedAtUtc = DateTime.UtcNow,
            Status = "InProgress"
        };

        try
        {
            _logger.LogInformation("[{JobName}] Starting sync pipeline", jobName);

            // STAGE 1: Fetch
            _logger.LogInformation("[{JobName}] FETCH stage starting", jobName);
            var products = await _apiClient.FetchAllProductsAsync(cancellationToken);
            result.ItemsFetched = products.Count;
            _logger.LogInformation(
                "[{JobName}] FETCH complete: {Count} records", jobName, result.ItemsFetched);

            if (result.ItemsFetched == 0)
            {
                _logger.LogWarning("[{JobName}] No products fetched. Skipping sync.", jobName);
                result.Status = "Success";
                result.CompletedAtUtc = DateTime.UtcNow;
                await _syncLogger.LogSyncAsync(result, cancellationToken);
                return result;
            }

            // STAGE 2: Stage into temp table
            _logger.LogInformation("[{JobName}] STAGE stage starting", jobName);
            var stagedCount = await _bulkRepository.StageProductsAsync(products, cancellationToken);
            _logger.LogInformation(
                "[{JobName}] STAGE complete: {Count} records staged", jobName, stagedCount);

            // STAGE 3: Merge
            _logger.LogInformation("[{JobName}] UPSERT stage starting", jobName);
            var (inserted, updated) = await _bulkRepository.MergeProductsAsync(cancellationToken);
            result.ItemsInserted = inserted;
            result.ItemsUpdated = updated;
            _logger.LogInformation(
                "[{JobName}] UPSERT complete: {Inserted} inserted, {Updated} updated",
                jobName, inserted, updated);

            // STAGE 4: Soft delete
            _logger.LogInformation("[{JobName}] SOFT DELETE stage starting", jobName);
            var deleted = await _bulkRepository.SoftDeleteMissingAsync(cancellationToken);
            result.ItemsDeleted = deleted;
            _logger.LogInformation(
                "[{JobName}] SOFT DELETE complete: {Deleted} records", jobName, deleted);

            // STAGE 5: Cleanup
            _logger.LogInformation("[{JobName}] CLEANUP stage starting", jobName);
            await _bulkRepository.CleanupTempTableAsync(cancellationToken);

            result.Status = "Success";
            result.CompletedAtUtc = DateTime.UtcNow;

            _logger.LogInformation(
                "[{JobName}] Sync pipeline complete. Fetched={Fetched}, " +
                "Inserted={Inserted}, Updated={Updated}, Deleted={Deleted}",
                jobName, result.ItemsFetched, result.ItemsInserted,
                result.ItemsUpdated, result.ItemsDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "[{JobName}] Sync pipeline failed: {Message}", jobName, ex.Message);

            result.Status = "Failed";
            result.ErrorMessage = $"{ex.Message}\n{ex.StackTrace}";
            result.CompletedAtUtc = DateTime.UtcNow;

            try
            {
                await _bulkRepository.CleanupTempTableAsync(CancellationToken.None);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx, "[{JobName}] Cleanup after failure also failed", jobName);
            }
        }

        // STAGE 6: Audit
        await _syncLogger.LogSyncAsync(result, CancellationToken.None);

        return result;
    }
}
