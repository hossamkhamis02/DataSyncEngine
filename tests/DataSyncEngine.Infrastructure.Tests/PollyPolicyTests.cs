using Polly;
using Polly.Registry;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using DataSyncEngine.Infrastructure.Http;
using Xunit;

namespace DataSyncEngine.Infrastructure.Tests;

public class PollyPolicyTests
{
    [Fact]
    public void ResiliencePipeline_IsRegisteredAndNotNull()
    {
        var services = new ServiceCollection();
        services.AddExternalApiPolicies();
        var provider = services.BuildServiceProvider();

        var registry = provider.GetRequiredService<ResiliencePipelineRegistry<string>>();
        var pipeline = registry.GetPipeline("ExternalApi");

        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void ResiliencePipeline_CanBeRetrievedByName()
    {
        var services = new ServiceCollection();
        services.AddExternalApiPolicies();
        var provider = services.BuildServiceProvider();

        var action = () =>
        {
            var registry = provider.GetRequiredService<ResiliencePipelineRegistry<string>>();
            return registry.GetPipeline("ExternalApi");
        };

        action.Should().NotThrow();
        action().Should().BeAssignableTo<ResiliencePipeline>();
    }
}
