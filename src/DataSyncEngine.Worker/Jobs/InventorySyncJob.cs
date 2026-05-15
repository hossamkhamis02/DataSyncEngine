using DataSyncEngine.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataSyncEngine.Worker.Jobs;

public class InventorySyncJob
{
    private readonly ISyncOrchestrator _orchestrator;
    private readonly ILogger<InventorySyncJob> _logger;

    public InventorySyncJob(
        ISyncOrchestrator orchestrator,
        ILogger<InventorySyncJob> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("InventorySyncJob triggered at {Time}", DateTime.UtcNow);

        var result = await _orchestrator.RunSyncAsync("InventorySyncJob");

        _logger.LogInformation(
            "InventorySyncJob completed: Status={Status}, " +
            "Fetched={Fetched}, Inserted={Inserted}, Updated={Updated}, Deleted={Deleted}, " +
            "Duration={Duration:F2}s",
            result.Status,
            result.ItemsFetched,
            result.ItemsInserted,
            result.ItemsUpdated,
            result.ItemsDeleted,
            (result.CompletedAtUtc - result.StartedAtUtc).TotalSeconds);
    }
}
