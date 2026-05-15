## Why

A production-quality .NET 8 Worker Service demonstrating enterprise-grade bulk data synchronization is needed as a GitHub portfolio project. This showcases advanced .NET backend skills — background processing, resilience patterns (Polly), high-performance database operations (SqlBulkCopy + MERGE), and clean architecture — which are critical in ERP integrations, CRM pipelines, and data warehouse scenarios.

## What Changes

- Create a new .NET 8 solution DataSyncEngine with 4 projects + 2 test projects
- Implement a background sync engine using Hangfire recurring jobs
- Build a typed HttpClient with Polly v8 retry + circuit breaker policies for external API calls
- Implement high-performance bulk upsert using SqlBulkCopy into temp tables + raw SQL MERGE
- Add structured logging with Serilog (console + file sinks)
- Provide a mock external API service generating 4,500 deterministic products across paginated responses
- Add EF Core 8 for schema management and migrations only (not for bulk data operations)
- Expose Hangfire dashboard on /hangfire for manual trigger and monitoring
- Add Docker Compose setup (Worker + SQL Server)
- Include comprehensive README with architecture diagram and local run instructions

## Capabilities

### New Capabilities
- sync-orchestrator: Coordinates the full sync flow (fetch, stage, upsert, soft-delete, cleanup, audit)
- xternal-api-client: Typed HttpClient with Polly v8 resilience policies for paginated REST API consumption
- ulk-repository: SqlBulkCopy staging + SQL MERGE for high-performance insert/update/soft-delete operations
- sync-audit: Structured sync logging (SyncLogs table) with inserted/updated/deleted counts and error capture
- mock-api-service: Deterministic in-memory mock API generating 4,500 paginated products with artificial latency
- hangfire-jobs: Recurring and manually-triggerable background job infrastructure with dashboard

### Modified Capabilities
- *(none — this is a greenfield project)*

## Impact

- **New Solution**: DataSyncEngine.sln with Worker, Core, Infrastructure, Contracts, and test projects
- **New Database Schema**: Products and SyncLogs tables managed via EF Core migrations
- **New Dependencies**: Hangfire, Polly, Serilog, EF Core 8, SQL Server client libraries
- **DevOps**: Docker Compose for local development; no impact on existing systems

