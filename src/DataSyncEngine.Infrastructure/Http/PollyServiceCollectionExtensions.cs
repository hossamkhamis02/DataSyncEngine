using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace DataSyncEngine.Infrastructure.Http;

public static class PollyServiceCollectionExtensions
{
    public static IServiceCollection AddExternalApiPolicies(this IServiceCollection services)
    {
        services.AddResiliencePipeline("ExternalApi", builder =>
        {
            builder.AddRetry(new()
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                UseJitter = true,
                BackoffType = DelayBackoffType.Exponential
            });

            builder.AddCircuitBreaker(new()
            {
                FailureRatio = 0.0,
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                SamplingDuration = TimeSpan.FromSeconds(30)
            });

            builder.AddTimeout(TimeSpan.FromSeconds(30));
        });

        return services;
    }
}
