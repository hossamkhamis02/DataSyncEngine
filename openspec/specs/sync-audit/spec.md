# sync-audit Specification

## Purpose
TBD - created by archiving change data-sync-engine. Update Purpose after archive.
## Requirements
### Requirement: Sync audit logger writes sync log records
The sync audit logger SHALL write a SyncLog record at the end of every sync attempt, capturing timing, status, and counts.

#### Scenario: Log successful sync
- **WHEN** a sync completes successfully
- **THEN** a SyncLog record SHALL be inserted with:
  - JobName = "InventorySyncJob"
  - StartedAtUtc = job start time
  - CompletedAtUtc = job completion time
  - Status = "Success"
  - ItemsFetched, ItemsInserted, ItemsUpdated, ItemsDeleted = actual counts
  - ErrorMessage = null

#### Scenario: Log failed sync
- **WHEN** a sync fails with an exception
- **THEN** a SyncLog record SHALL be inserted with:
  - Status = "Failed"
  - ErrorMessage = exception message and stack trace (truncated if necessary)
  - ItemsFetched = count fetched before failure (if known), otherwise 0

### Requirement: Sync log table schema
The SyncLogs table SHALL have the following columns: Id (int, PK, identity), JobName (nvarchar), StartedAtUtc (datetime2), CompletedAtUtc (datetime2), Status (nvarchar), ItemsFetched (int), ItemsInserted (int), ItemsUpdated (int), ItemsDeleted (int), ErrorMessage (nvarchar, max).

#### Scenario: Verify table columns
- **WHEN** a SyncLog record is written
- **THEN** it SHALL contain all specified columns with their correct data types
- **AND** Id SHALL be the primary key with auto-increment

