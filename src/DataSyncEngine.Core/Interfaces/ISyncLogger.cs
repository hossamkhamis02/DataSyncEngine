using DataSyncEngine.Contracts;

namespace DataSyncEngine.Core.Interfaces;

public interface ISyncLogger
{
    Task LogSyncAsync(SyncResult result, CancellationToken cancellationToken = default);
}
