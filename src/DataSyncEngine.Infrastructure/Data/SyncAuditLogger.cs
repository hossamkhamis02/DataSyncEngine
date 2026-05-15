using DataSyncEngine.Contracts;
using DataSyncEngine.Core.Entities;
using DataSyncEngine.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DataSyncEngine.Infrastructure.Data;

public class SyncAuditLogger : ISyncLogger
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SyncAuditLogger> _logger;

    public SyncAuditLogger(AppDbContext dbContext, ILogger<SyncAuditLogger> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task LogSyncAsync(SyncResult result, CancellationToken cancellationToken = default)
    {
        var syncLog = new SyncLog
        {
            JobName = result.JobName,
            StartedAtUtc = result.StartedAtUtc,
            CompletedAtUtc = result.CompletedAtUtc,
            Status = result.Status,
            ItemsFetched = result.ItemsFetched,
            ItemsInserted = result.ItemsInserted,
            ItemsUpdated = result.ItemsUpdated,
            ItemsDeleted = result.ItemsDeleted,
            ErrorMessage = result.ErrorMessage is not null && result.ErrorMessage.Length > 4000
                ? result.ErrorMessage[..4000]
                : result.ErrorMessage
        };

        _dbContext.SyncLogs.Add(syncLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Sync audit logged: Job={JobName}, Status={Status}, " +
            "Fetched={Fetched}, Inserted={Inserted}, Updated={Updated}, Deleted={Deleted}",
            result.JobName, result.Status, result.ItemsFetched,
            result.ItemsInserted, result.ItemsUpdated, result.ItemsDeleted);
    }
}
