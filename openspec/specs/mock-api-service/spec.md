# mock-api-service Specification

## Purpose
TBD - created by archiving change data-sync-engine. Update Purpose after archive.
## Requirements
### Requirement: Mock API generates deterministic paginated data
The mock API service SHALL generate exactly 4,500 product records deterministically (same data per run) across paginated responses.

#### Scenario: Generate paginated products
- **WHEN** a page is requested with page number and page size
- **THEN** the service SHALL return the correct slice of 4,500 total records
- **AND** each record SHALL contain: ExternalId, Name, CategoryCode, Price, StockQuantity, IsActive, LastModifiedUtc
- **AND** the response SHALL include page, pageSize, totalCount, and items

#### Scenario: Simulate network latency
- **WHEN** any page is requested
- **THEN** the service SHALL delay the response by 50ms to simulate real network latency

### Requirement: Mock API is swappable via feature flag
The application SHALL use the mock API when UseMockApi in appsettings.json is true, and use a real HttpClient when false.

#### Scenario: Toggle mock API via configuration
- **WHEN** UseMockApi is set to true
- **THEN** the IExternalApiClient implementation SHALL resolve to the mock service
- **AND** when set to false, it SHALL resolve to the real typed HttpClient

