namespace DataSyncEngine.Core.Entities;

public class SyncLog
{
    public int Id { get; set; }
    public string JobName { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemsFetched { get; set; }
    public int ItemsInserted { get; set; }
    public int ItemsUpdated { get; set; }
    public int ItemsDeleted { get; set; }
    public string? ErrorMessage { get; set; }
}
