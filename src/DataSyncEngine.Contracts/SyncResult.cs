namespace DataSyncEngine.Contracts;

public class SyncResult
{
    public string JobName { get; set; } = string.Empty;
    public int ItemsFetched { get; set; }
    public int ItemsInserted { get; set; }
    public int ItemsUpdated { get; set; }
    public int ItemsDeleted { get; set; }
    public string Status { get; set; } = "Unknown";
    public string? ErrorMessage { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime CompletedAtUtc { get; set; }
}
