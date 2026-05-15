using DataSyncEngine.Contracts;

namespace DataSyncEngine.Core.Interfaces;

public interface ISyncOrchestrator
{
    Task<SyncResult> RunSyncAsync(string jobName, CancellationToken cancellationToken = default);
}
