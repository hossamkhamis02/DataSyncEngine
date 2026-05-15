using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace DataSyncEngine.Infrastructure.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetSection("SyncConfiguration")["ConnectionString"];

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}
