## ADDED Requirements

### Requirement: Sync orchestrator coordinates full sync pipeline
The sync orchestrator SHALL coordinate the complete data synchronization flow in the following order: FETCH, STAGE, UPSERT, SOFT DELETE, CLEANUP, and AUDIT.

#### Scenario: Successful full sync execution
- **WHEN** the sync job is triggered
- **THEN** the orchestrator SHALL execute each stage sequentially
- **AND** log progress at each stage (e.g., "Fetching page X/Y", "Staging N records")
- **AND** return a sync summary with inserted, updated, and deleted counts

#### Scenario: Sync failure at any stage
- **WHEN** an exception occurs during any stage
- **THEN** the orchestrator SHALL abort the remaining stages
- **AND** log the failure with the stage name and exception details
- **AND** write an audit record with Status = Failed and the error message

### Requirement: Sync orchestrator is idempotent
The sync orchestrator SHALL be safe to run multiple times without causing duplicate data or inconsistent state.

#### Scenario: Running sync job multiple times
- **WHEN** the same sync job is triggered twice in succession
- **THEN** the second run SHALL produce the same final database state as the first run
- **AND** the SyncLog SHALL contain two independent audit records
