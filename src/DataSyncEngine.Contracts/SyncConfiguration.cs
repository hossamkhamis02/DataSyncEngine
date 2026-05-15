namespace DataSyncEngine.Contracts;

public class SyncConfiguration
{
    public const string SectionName = "SyncConfiguration";

    public int PageSize { get; set; } = 100;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public int RetryCount { get; set; } = 3;
    public int CircuitBreakerThreshold { get; set; } = 5;
    public int CircuitBreakerDurationSeconds { get; set; } = 30;
    public string SyncCronExpression { get; set; } = "0 * * * *";
    public bool UseMockApi { get; set; } = true;
    public string ConnectionString { get; set; } = string.Empty;
}
