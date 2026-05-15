using DataSyncEngine.Infrastructure;
using DataSyncEngine.Worker.Jobs;
using Hangfire;
using Hangfire.SqlServer;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/data-sync-engine-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();

builder.Host.UseSerilog();

var syncConfig = builder.Configuration.GetSection("SyncConfiguration");
var connectionString = syncConfig["ConnectionString"]!;

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
          {
              CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
              SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
              QueuePollInterval = TimeSpan.FromSeconds(15),
              UseRecommendedIsolationLevel = true,
              DisableGlobalLocks = true
          });
});

builder.Services.AddHangfireServer();

builder.Services.AddScoped<InventorySyncJob>();

var app = builder.Build();

app.MapHangfireDashboard("/hangfire");

app.MapGet("/", () => "DataSyncEngine Worker is running. Dashboard: /hangfire");

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

using (var scope = app.Services.CreateScope())
{
    var recurringJob = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    var cronExpression = syncConfig["SyncCronExpression"] ?? "0 * * * *";

    recurringJob.AddOrUpdate<InventorySyncJob>(
        "InventorySyncJob",
        job => job.ExecuteAsync(),
        cronExpression);

    Log.Information(
        "Registered recurring job InventorySyncJob with cron: {Cron}", cronExpression);
}

try
{
    Log.Information("Starting DataSyncEngine Worker");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
