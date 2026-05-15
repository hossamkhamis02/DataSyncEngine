## Context

This is a greenfield .NET 8 Worker Service project built as a portfolio piece demonstrating enterprise data integration patterns. The system synchronizes product inventory from an external paginated REST API into a local SQL Server database using high-performance bulk operations. It must run reliably as a background service, handle transient failures gracefully, and provide observability through structured logging and a job dashboard.

**Current State**: No existing codebase. This is a new solution from scratch.
**Constraints**: 
- Must be runnable locally without external API dependencies (mock API provided)
- Must use .NET 8 Worker Service (not ASP.NET Core Web API)
- Must demonstrate clean architecture with testable abstractions
- Must showcase high-performance bulk DB operations without EF Core for data paths

## Goals / Non-Goals

**Goals:**
- Implement a complete, production-quality bulk data sync engine
- Demonstrate clean architecture with clear separation of concerns
- Showcase Polly v8 resilience patterns on HTTP calls
- Use SqlBulkCopy + SQL MERGE for high-performance upserts and soft deletes
- Provide deterministic mock API for idempotent testing and demo purposes
- Expose Hangfire dashboard for job monitoring and manual triggers
- Include Docker Compose for one-command local startup
- Add comprehensive README with architecture diagram

**Non-Goals:**
- Real-time streaming or event-driven architecture (batch sync only)
- Multi-tenancy or user management
- Authentication/authorization on the Hangfire dashboard (basic exposure is sufficient for portfolio)
- Actual production deployment configuration (local/dev focus)
- Support for databases other than SQL Server
- Using EF Core for bulk data operations (schema management only)

## Decisions

### Decision 1: Worker Service over ASP.NET Core Web API
**Rationale**: A Worker Service (`IHostedService`) is the correct abstraction for background processing. While Hangfire can run in a Web API, a Worker Service more accurately represents the headless nature of integration engines. The Hangfire dashboard will be exposed via `AddHangfireServer` + `UseHangfireDashboard` on a minimal Kestrel endpoint within the worker.

### Decision 2: SqlBulkCopy + Temp Table + MERGE instead of EF Core AddRange
**Rationale**: EF Core `AddRange` + `SaveChanges` is prohibitively slow for 4,500+ records (row-by-row INSERT/UPDATE). `SqlBulkCopy` into a temp table followed by a SQL `MERGE` statement is the industry-standard pattern for high-performance upserts in SQL Server. This keeps the data path fast while still using EF Core migrations for schema management.

### Decision 3: DataTable over IDataReader for SqlBulkCopy
**Rationale**: While `IDataReader` is more memory-efficient, `DataTable` is significantly more readable and maintainable for a portfolio project. The dataset size (4,500 records) is small enough that `DataTable` memory overhead is negligible. This prioritizes code clarity over micro-optimizations.

### Decision 4: Mock API as a swappable service with feature flag
**Rationale**: To make the project immediately runnable without external dependencies, a deterministic mock API service generates 4,500 products in-memory. It is swappable with a real `HttpClient` via `UseMockApi: true/false` in `appsettings.json`. This keeps the demo self-contained while demonstrating the real HTTP client pattern.

### Decision 5: Polly v8 configured via extension method `AddExternalApiPolicies()`
**Rationale**: Centralizing Polly policy configuration in a reusable extension method keeps `Program.cs` clean and makes policies testable in isolation. The v8 API uses `ResiliencePipeline` which is the modern approach.

### Decision 6: EF Core Migrations for schema, raw SQL for data operations
**Rationale**: EF Core provides excellent schema versioning via migrations. However, for the sync data path, raw ADO.NET (`SqlConnection`, `SqlBulkCopy`, `SqlCommand`) is used. This is a common hybrid pattern in enterprise systems where EF manages the schema but high-performance operations bypass it.

### Decision 7: Soft deletes with IsDeleted flag
**Rationale**: When products disappear from the external API, they should not be hard-deleted from the local database. A soft delete (`IsDeleted = 1`) preserves historical data and referential integrity, which is standard practice in data warehouse and ERP integrations.

### Decision 8: Serilog with Console + File sinks
**Rationale**: Structured logging is essential for background services where you cannot attach a debugger. Serilog is the de facto standard in .NET. Console sink for Docker/container visibility, File sink for persistence.

## Risks / Trade-offs

- **[Risk] SqlBulkCopy requires SQL Server-specific syntax** → **Mitigation**: This is acceptable as SQL Server is the stated target. The `IBulkRepository` abstraction allows future providers if needed.
- **[Risk] Hangfire in-memory storage loses jobs on restart** → **Mitigation**: Configure Hangfire with SQL Server storage in `appsettings.json`. Default to in-memory only for the simplest local run, but document the SQL Server storage option.
- **[Risk] Mock API does not simulate true network failures** → **Mitigation**: Polly policies are still fully configured and can be tested by injecting a faulty `HttpClient` or by extending the mock to simulate failures.
- **[Risk] DataTable memory usage for very large datasets** → **Mitigation**: For 4,500 records this is negligible. If scaling beyond 100k records, switch to `IDataReader` or batch the sync into chunks.
- **[Trade-off] Minimal API endpoint for Hangfire dashboard adds slight complexity** → **Acceptance**: This is a necessary compromise to expose the dashboard in a Worker Service. The endpoint is thin and contains zero business logic.
