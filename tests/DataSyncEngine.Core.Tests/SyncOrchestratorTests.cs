using DataSyncEngine.Contracts;
using DataSyncEngine.Core.Interfaces;
using DataSyncEngine.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataSyncEngine.Core.Tests;

public class SyncOrchestratorTests
{
    private readonly Mock<IExternalApiClient> _apiClientMock;
    private readonly Mock<IBulkRepository> _bulkRepositoryMock;
    private readonly Mock<ISyncLogger> _syncLoggerMock;
    private readonly Mock<ILogger<SyncOrchestrator>> _loggerMock;
    private readonly SyncOrchestrator _orchestrator;

    public SyncOrchestratorTests()
    {
        _apiClientMock = new Mock<IExternalApiClient>();
        _bulkRepositoryMock = new Mock<IBulkRepository>();
        _syncLoggerMock = new Mock<ISyncLogger>();
        _loggerMock = new Mock<ILogger<SyncOrchestrator>>();
        _orchestrator = new SyncOrchestrator(
            _apiClientMock.Object,
            _bulkRepositoryMock.Object,
            _syncLoggerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RunSync_SuccessfulSync_ReturnsSuccessResult()
    {
        var products = new List<ExternalProductDto>
        {
            new() { ExternalId = "SKU-001", Name = "Test", CategoryCode = "ELEC", Price = 10, StockQuantity = 5, IsActive = true }
        };

        _apiClientMock.Setup(x => x.FetchAllProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products.AsReadOnly());
        _bulkRepositoryMock.Setup(x => x.StageProductsAsync(products, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _bulkRepositoryMock.Setup(x => x.MergeProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, 0));
        _bulkRepositoryMock.Setup(x => x.SoftDeleteMissingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _syncLoggerMock.Setup(x => x.LogSyncAsync(It.IsAny<SyncResult>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _orchestrator.RunSyncAsync("TestJob");

        result.Status.Should().Be("Success");
        result.ItemsFetched.Should().Be(1);
        result.ItemsInserted.Should().Be(1);
    }

    [Fact]
    public async Task RunSync_EmptyFetch_ReturnsSuccessWithZeroCounts()
    {
        _apiClientMock.Setup(x => x.FetchAllProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalProductDto>().AsReadOnly());

        var result = await _orchestrator.RunSyncAsync("TestJob");

        result.Status.Should().Be("Success");
        result.ItemsFetched.Should().Be(0);
        result.ItemsInserted.Should().Be(0);
    }

    [Fact]
    public async Task RunSync_ApiFailure_EnsuresCleanupAndLogsFailure()
    {
        _apiClientMock.Setup(x => x.FetchAllProductsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var result = await _orchestrator.RunSyncAsync("TestJob");

        result.Status.Should().Be("Failed");
        result.ErrorMessage.Should().NotBeNull();
        result.ErrorMessage.Should().Contain("Connection failed");
        _bulkRepositoryMock.Verify(x => x.CleanupTempTableAsync(It.IsAny<CancellationToken>()), Times.Once);
        _syncLoggerMock.Verify(x => x.LogSyncAsync(It.IsAny<SyncResult>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunSync_StagesExecuteInCorrectOrder()
    {
        var callOrder = new List<string>();
        var products = new List<ExternalProductDto>
        {
            new() { ExternalId = "SKU-001", Name = "Test", CategoryCode = "ELEC", Price = 10, StockQuantity = 5, IsActive = true }
        };

        _apiClientMock.Setup(x => x.FetchAllProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products.AsReadOnly())
            .Callback(() => callOrder.Add("FETCH"));
        _bulkRepositoryMock.Setup(x => x.StageProductsAsync(products, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => callOrder.Add("STAGE"));
        _bulkRepositoryMock.Setup(x => x.MergeProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, 0))
            .Callback(() => callOrder.Add("MERGE"));
        _bulkRepositoryMock.Setup(x => x.SoftDeleteMissingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0)
            .Callback(() => callOrder.Add("SOFT_DELETE"));
        _bulkRepositoryMock.Setup(x => x.CleanupTempTableAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("CLEANUP"));

        await _orchestrator.RunSyncAsync("TestJob");

        callOrder.Should().ContainInOrder("FETCH", "STAGE", "MERGE", "SOFT_DELETE", "CLEANUP");
    }

    [Fact]
    public async Task RunSync_UpdatedAndInsertedCounts_ReflectReality()
    {
        var products = new List<ExternalProductDto>
        {
            new() { ExternalId = "SKU-001", Name = "A", CategoryCode = "ELEC", Price = 10, StockQuantity = 5, IsActive = true },
            new() { ExternalId = "SKU-002", Name = "B", CategoryCode = "HOME", Price = 20, StockQuantity = 3, IsActive = true }
        };

        _apiClientMock.Setup(x => x.FetchAllProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products.AsReadOnly());
        _bulkRepositoryMock.Setup(x => x.StageProductsAsync(products, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _bulkRepositoryMock.Setup(x => x.MergeProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((2, 3));
        _bulkRepositoryMock.Setup(x => x.SoftDeleteMissingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await _orchestrator.RunSyncAsync("TestJob");

        result.ItemsFetched.Should().Be(2);
        result.ItemsInserted.Should().Be(2);
        result.ItemsUpdated.Should().Be(3);
        result.ItemsDeleted.Should().Be(5);
    }
}
