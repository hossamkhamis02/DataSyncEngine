## 1. Solution Scaffold & Shared Configuration

- [x] 1.1 Create .NET 8 solution `DataSyncEngine.sln` with 6 projects: Worker, Core, Infrastructure, Contracts, Core.Tests, Infrastructure.Tests
- [x] 1.2 Add project references: Worker → Infrastructure → Core; Infrastructure → Contracts; Core → Contracts; Test projects → their target + Moq/xUnit
- [x] 1.3 Create `SyncConfiguration` POCO bound from `appsettings.json` (PageSize, ApiBaseUrl, RetryCount, CircuitBreakerThreshold, SyncCronExpression, UseMockApi, ConnectionString)
- [x] 1.4 Add `.gitignore` (standard .NET), `.env.example`, `appsettings.json`, and `appsettings.Development.json` with all configuration keys
- [x] 1.5 Enable nullable reference types across all projects

## 2. Contracts Layer

- [x] 2.1 Define `ExternalProductDto` with properties: ExternalId, Name, CategoryCode, Price, StockQuantity, IsActive, LastModifiedUtc
- [x] 2.2 Define `PaginatedResponse<T>` with: page, pageSize, totalCount, items
- [x] 2.3 Define `SyncResult` with: ItemsFetched, ItemsInserted, ItemsUpdated, ItemsDeleted, Status, ErrorMessage

## 3. Core Layer — Entities & Interfaces

- [x] 3.1 Create `Product` entity: Id, ExternalId, Name, CategoryCode, Price, StockQuantity, IsActive, IsDeleted, LastSyncedAtUtc, CreatedAtUtc, UpdatedAtUtc
- [x] 3.2 Create `SyncLog` entity: Id, JobName, StartedAtUtc, CompletedAtUtc, Status, ItemsFetched, ItemsInserted, ItemsUpdated, ItemsDeleted, ErrorMessage
- [x] 3.3 Define `IExternalApiClient` interface with `Task<IReadOnlyList<ExternalProductDto>> FetchAllProductsAsync(CancellationToken)`
- [x] 3.4 Define `IBulkRepository` interface with methods: StageProductsAsync, MergeProductsAsync, SoftDeleteMissingAsync, CleanupTempTableAsync
- [x] 3.5 Define `ISyncLogger` interface with `Task LogSyncAsync(SyncResult result)`
- [x] 3.6 Define `ISyncOrchestrator` interface with `Task<SyncResult> RunSyncAsync(string jobName, CancellationToken)`

## 4. Infrastructure Layer — EF Core & Migrations

- [x] 4.1 Create `AppDbContext` with `DbSet<Product>` and `DbSet<SyncLog>`
- [x] 4.2 Configure `Product` entity: unique index on ExternalId, default values for CreatedAtUtc/UpdatedAtUtc
- [x] 4.3 Configure `SyncLog` entity with max length constraints
- [x] 4.4 Create initial EF Core migration (`InitialCreate`) generating Products and SyncLogs tables
- [x] 4.5 Seed `SyncConfiguration` defaults via `IConfiguration` binding (not DB seed)

## 5. Infrastructure Layer — Polly Policies & HTTP Client

- [x] 5.1 Create `AddExternalApiPolicies()` extension on `IServiceCollection` using Polly v8
- [x] 5.2 Configure retry policy: 3 attempts, exponential backoff (2s, 4s, 8s) with jitter on transient HTTP errors
- [x] 5.3 Configure circuit breaker: open after 5 consecutive failures, 30s break duration, half-open after cooldown
- [x] 5.4 Register typed `HttpClient` (`ExternalApiClient`) with `IHttpClientFactory` and attach Polly resilience pipeline
- [x] 5.5 Implement `ExternalApiClient` consuming paginated API, aggregating all pages into `IReadOnlyList<ExternalProductDto>`

## 6. Infrastructure Layer — Mock API Service

- [x] 6.1 Implement `MockExternalApiService` implementing `IExternalApiClient`
- [x] 6.2 Generate 4,500 deterministic products using seeded random (same data per run) with realistic categories and prices
- [x] 6.3 Simulate pagination (100 items/page = 45 pages) with artificial 50ms delay per page
- [x] 6.4 Register mock service conditionally when `UseMockApi: true` in `SyncConfiguration`

## 7. Infrastructure Layer — Bulk Repository

- [x] 7.1 Implement `SqlServerBulkRepository` with `SqlConnection` factory from configuration
- [x] 7.2 Implement `StageProductsAsync`: create `##TempProducts` temp table, populate `DataTable`, bulk insert via `SqlBulkCopy`
- [x] 7.3 Implement `MergeProductsAsync`: execute raw SQL MERGE from `##TempProducts` to `Products` with INSERT/UPDATE logic and output counts
- [x] 7.4 Implement `SoftDeleteMissingAsync`: UPDATE Products SET IsDeleted=1 WHERE ExternalId NOT IN (SELECT ExternalId FROM ##TempProducts)
- [x] 7.5 Implement `CleanupTempTableAsync`: `DROP TABLE IF EXISTS ##TempProducts` wrapped in try/finally pattern
- [x] 7.6 Write full MERGE SQL string: compare ExternalId, update Name/CategoryCode/Price/StockQuantity/IsActive/UpdatedAtUtc, insert with CreatedAtUtc/UpdatedAtUtc

## 8. Infrastructure Layer — Sync Audit Logger

- [x] 8.1 Implement `SyncAuditLogger` using `AppDbContext` to write `SyncLog` records
- [x] 8.2 On success: log Status="Success" with all counts and timing
- [x] 8.3 On failure: log Status="Failed" with exception message (truncated to column limit) and counts fetched before failure

## 9. Core Layer — Sync Orchestrator

- [x] 9.1 Implement `SyncOrchestrator` coordinating FETCH → STAGE → UPSERT → SOFT DELETE → CLEANUP → AUDIT
- [x] 9.2 Log progress at each stage ("Fetching page X/Y", "Staging N records", "Merged: X inserted, Y updated", "Soft-deleted: Z")
- [x] 9.3 Catch exceptions, abort remaining stages, ensure cleanup runs via try/finally, write failed audit log
- [x] 9.4 Return `SyncResult` with final counts and status
- [x] 9.5 Ensure idempotency: multiple runs produce same DB state (MERGE handles duplicates naturally)

## 10. Worker Layer — Hangfire & Hosting

- [x] 10.1 Create `InventorySyncJob` class with `ExecuteAsync()` method invoking `ISyncOrchestrator.RunSyncAsync("InventorySyncJob", ...)`
- [x] 10.2 Configure Hangfire with SQL Server storage (or in-memory for simplest local run, configurable)
- [x] 10.3 Register recurring job "InventorySyncJob" with cron from `SyncConfiguration.SyncCronExpression`
- [x] 10.4 Add minimal Kestrel endpoint exposing Hangfire dashboard on `/hangfire`
- [x] 10.5 Configure Serilog with Console and File sinks, enrich with Hangfire job context
- [x] 10.6 Wire up DI in `Program.cs`: register all interfaces → implementations, add Polly policies, add Hangfire, add DbContext

## 11. Tests Skeleton

- [x] 11.1 Create `DataSyncEngine.Core.Tests` with xUnit, Moq, FluentAssertions
- [x] 11.2 Create `DataSyncEngine.Infrastructure.Tests` with xUnit, Moq, FluentAssertions, Testcontainers (or LocalDb) for integration tests
- [x] 11.3 Add `MockExternalApiServiceTests`: verify deterministic output, pagination, latency simulation
- [x] 11.4 Add `SyncOrchestratorTests` (mocked dependencies): verify stage order, failure handling, result counts
- [x] 11.5 Add `SqlServerBulkRepositoryTests` (integration): verify staging, MERGE, soft delete, cleanup against LocalDb
- [x] 11.6 Add `PollyPolicyTests`: verify retry count, backoff timing, circuit breaker state transitions

## 12. DevOps & Documentation

- [x] 12.1 Create `docker-compose.yml` with SQL Server 2022 container + Worker Service build
- [x] 12.2 Write `README.md` with: project overview, ASCII architecture flow diagram, tech stack badges, how to run locally (dotnet run + docker-compose), Hangfire dashboard URL, layer descriptions
- [x] 12.3 Verify solution builds cleanly (`dotnet build`)
- [x] 12.4 Verify EF migration applies (`dotnet ef database update`)
- [x] 12.5 Verify Worker starts, Hangfire dashboard accessible, mock sync completes end-to-end with logs
