using DataSyncEngine.Core.Interfaces;
using DataSyncEngine.Core.Services;
using DataSyncEngine.Infrastructure.Data;
using DataSyncEngine.Infrastructure.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataSyncEngine.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var syncConfig = configuration.GetSection("SyncConfiguration");
        var connectionString = syncConfig["ConnectionString"]!;

        services.AddInfrastructureData(configuration);

        services.AddScoped<IBulkRepository>(sp =>
            new SqlServerBulkRepository(
                connectionString,
                sp.GetRequiredService<ILogger<SqlServerBulkRepository>>()));

        services.AddScoped<ISyncLogger, SyncAuditLogger>();

        services.AddExternalApiPolicies();

        if (syncConfig.GetValue<bool>("UseMockApi"))
        {
            services.AddSingleton<IExternalApiClient, MockExternalApiService>();
        }
        else
        {
            services.AddHttpClient<IExternalApiClient, ExternalApiClient>(client =>
            {
                client.BaseAddress = new Uri(syncConfig["ApiBaseUrl"]!);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }

        services.AddScoped<ISyncOrchestrator, SyncOrchestrator>();

        return services;
    }
}
