# DataSyncEngine

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=flat&logo=microsoft-sql-server)](https://www.microsoft.com/sql-server)
[![Polly](https://img.shields.io/badge/Polly-v8-6C40AA?style=flat)](https://www.thepollyproject.org/)
[![Hangfire](https://img.shields.io/badge/Hangfire-1.8-FE6B23?style=flat)](https://www.hangfire.io/)
[![Serilog](https://img.shields.io/badge/Serilog-structured-6B57AB?style=flat)](https://serilog.net/)
[![License](https://img.shields.io/badge/license-MIT-blue?style=flat)](LICENSE)

A production-quality .NET 8 Worker Service demonstrating enterprise-grade bulk data synchronization. Fetches product inventory from a paginated external REST API (simulated) and syncs it into SQL Server using high-performance bulk operations (SqlBulkCopy + SQL MERGE).

## Architecture

```
                   ┌─────────────────────────────────────────────┐
                   │           DataSyncEngine.Worker             │
                   │  ┌───────────────────┐  ┌────────────────┐ │
                   │  │ Hangfire Dashboard │  │ InventorySyncJob│ │
                   │  │   /hangfire        │  │  (Recurring)    │ │
                   │  └───────────────────┘  └───────┬────────┘ │
                   └─────────────────────────────────┼──────────┘
                                                     │
                   ┌─────────────────────────────────┼──────────┐
                   │       DataSyncEngine.Core      │          │
                   │  ┌──────────────────────────────▼──┐       │
                   │  │       SyncOrchestrator          │       │
                   │  │   FETCH → STAGE → UPSERT →      │       │
                   │  │   SOFT DELETE → CLEANUP → AUDIT │       │
                   │  └─────────────────────────────────┘       │
                   │  IExternalApiClient  IBulkRepository        │
                   │  ISyncLogger          SyncResult            │
                   └──────────┬──────────────────┬──────────────┘
                              │                  │
         ┌────────────────────▼──┐    ┌──────────▼──────────────┐
         │   Infrastructure      │    │   Infrastructure         │
         │  ┌──────────────────┐ │    │  ┌────────────────────┐  │
         │  │ ExternalApiClient │ │    │  │SqlServerBulkRepo   │  │
         │  │ (Typed HttpClient)│ │    │  │  SqlBulkCopy       │  │
         │  │ + Polly v8        │ │    │  │  ##TempProducts    │  │
         │  │ + Circuit Breaker │ │    │  │  SQL MERGE         │  │
         │  └──────────────────┘ │    │  └────────────────────┘  │
         │  ┌──────────────────┐ │    │  ┌────────────────────┐  │
         │  │MockApiService    │ │    │  │  SyncAuditLogger   │  │
         │  │ 4,500 products   │ │    │  │  (EF Core)         │  │
         │  │ Deterministic    │ │    │  └────────────────────┘  │
         │  └──────────────────┘ │    │  ┌────────────────────┐  │
         │                       │    │  │  AppDbContext      │  │
         │                       │    │  │  (Schema/Migrations│  │
         │                       │    │  └────────────────────┘  │
         └───────────────────────┘    └──────────────────────────┘
                              │                  │
                              └────────┬─────────┘
                                       │
                         ┌─────────────▼──────────────┐
                         │       SQL Server            │
                         │  ┌───────────────────────┐  │
                         │  │  Products Table        │  │
                         │  │  SyncLogs Table        │  │
                         │  │  Hangfire Schema       │  │
                         │  └───────────────────────┘  │
                         └────────────────────────────┘
```

### Solution Structure

```
src/
  DataSyncEngine.Worker/         # Kestrel host, Hangfire dashboard, Program.cs
  DataSyncEngine.Core/           # Entities, Interfaces, SyncOrchestrator
  DataSyncEngine.Infrastructure/ # SqlBulkCopy, EF Core, HTTP clients, Polly
  DataSyncEngine.Contracts/      # DTOs, SyncResult, SyncConfiguration
tests/
  DataSyncEngine.Core.Tests/
  DataSyncEngine.Infrastructure.Tests/
```

## Sync Pipeline (6 Stages)

| Stage | Description | Technology |
|-------|-------------|-----------|
| **FETCH** | Retrieve all products from external API | Typed HttpClient + Polly v8 |
| **STAGE** | Bulk load into `##TempProducts` | `SqlBulkCopy` + `DataTable` |
| **UPSERT** | Merge temp table into `Products` | Raw SQL `MERGE` statement |
| **SOFT DELETE** | Mark removed products as `IsDeleted=1` | Raw SQL `UPDATE` |
| **CLEANUP** | Drop `##TempProducts` | `DROP TABLE IF EXISTS` |
| **AUDIT** | Write sync summary to `SyncLogs` | EF Core |

## Key Patterns

- **SqlBulkCopy + Temp Table + MERGE**: Industry-standard pattern for high-performance upserts. Not EF `AddRange` — this handles 4,500+ records in sub-second time.
- **Polly v8 Resilience**: Retry (3 attempts, exponential backoff with jitter) + Circuit Breaker (5 failures → 30s open) on all HTTP calls.
- **Soft Deletes**: Products removed from the source API are flagged `IsDeleted=1` rather than hard-deleted, preserving historical data.
- **Idempotent Jobs**: Safe to run multiple times — the `MERGE` naturally handles duplicates, and `SyncLogs` record each independent run.
- **Mock API**: Deterministic in-memory API generating 4,500 products. Swappable with real HTTP client via `UseMockApi: true/false`.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) (local or Docker)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for docker-compose)

## Quick Start

### Option 1: Docker Compose (Recommended)

```bash
# Start SQL Server + Worker
docker-compose up -d

# Apply migrations
docker-compose exec worker dotnet ef database update

# Open Hangfire Dashboard
# http://localhost:8080/hangfire
```

### Option 2: Local Development

```bash
# 1. Clone and restore
git clone <repo-url>
cd data-sync-engine
dotnet restore

# 2. Update connection string in appsettings.Development.json
#    (or use dotnet user-secrets)

# 3. Apply EF Core migrations
dotnet ef database update \
  --project src/DataSyncEngine.Infrastructure \
  --startup-project src/DataSyncEngine.Worker

# 4. Run the worker
dotnet run --project src/DataSyncEngine.Worker

# 5. Open Hangfire Dashboard
# http://localhost:8080/hangfire
```

## Configuration

| Key | Default | Description |
|-----|---------|-------------|
| `SyncConfiguration:PageSize` | `100` | Records per page from external API |
| `SyncConfiguration:ApiBaseUrl` | (empty) | External API base URL (not used when `UseMockApi=true`) |
| `SyncConfiguration:RetryCount` | `3` | HTTP retry attempts |
| `SyncConfiguration:CircuitBreakerThreshold` | `5` | Failures before circuit opens |
| `SyncConfiguration:CircuitBreakerDurationSeconds` | `30` | Circuit breaker open duration |
| `SyncConfiguration:SyncCronExpression` | `0 * * * *` | Hangfire cron (hourly) |
| `SyncConfiguration:UseMockApi` | `true` | Use in-memory mock API |
| `SyncConfiguration:ConnectionString` | (required) | SQL Server connection string |

## Endpoints

| Path | Description |
|------|-------------|
| `/` | Health info + dashboard link |
| `/health` | JSON health check |
| `/hangfire` | Hangfire dashboard (manual trigger, job history) |

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Runtime** | .NET 8 |
| **Background Jobs** | Hangfire 1.8 |
| **HTTP Resilience** | Polly v8 (retry + circuit breaker) |
| **Database** | SQL Server 2022 |
| **ORM (schema only)** | EF Core 8 |
| **Bulk Operations** | SqlBulkCopy + Raw SQL MERGE |
| **Logging** | Serilog (console + file) |
| **Testing** | xUnit, Moq, FluentAssertions |
| **Containerization** | Docker Compose |

## License

MIT
