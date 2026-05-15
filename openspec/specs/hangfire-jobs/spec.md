# hangfire-jobs Specification

## Purpose
TBD - created by archiving change data-sync-engine. Update Purpose after archive.
## Requirements
### Requirement: Recurring inventory sync job
The application SHALL register a recurring Hangfire job named "InventorySyncJob" that runs on a configurable cron schedule (default: every hour).

#### Scenario: Job runs on cron schedule
- **WHEN** the Worker Service starts
- **THEN** Hangfire SHALL register "InventorySyncJob" with the cron expression from SyncConfiguration:SyncCronExpression
- **AND** the job SHALL invoke the sync orchestrator on each scheduled run

#### Scenario: Manual trigger via dashboard
- **WHEN** a user navigates to /hangfire
- **THEN** the Hangfire dashboard SHALL display the recurring job
- **AND** the user SHALL be able to trigger the job manually

### Requirement: Job idempotency
The inventory sync job SHALL be idempotent — safe to run multiple times without adverse effects.

#### Scenario: Trigger job multiple times manually
- **WHEN** the job is triggered manually two times in a row
- **THEN** both runs SHALL complete successfully
- **AND** the final database state SHALL be consistent
- **AND** two separate SyncLog records SHALL exist

