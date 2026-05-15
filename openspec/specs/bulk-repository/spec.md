# bulk-repository Specification

## Purpose
TBD - created by archiving change data-sync-engine. Update Purpose after archive.
## Requirements
### Requirement: Bulk repository stages data via SqlBulkCopy
The bulk repository SHALL stage fetched records into a global temporary table (##TempProducts) using SqlBulkCopy with a DataTable.

#### Scenario: Stage 4500 records into temp table
- **WHEN** 4500 product records are provided for staging
- **THEN** the repository SHALL create ##TempProducts if it does not exist
- **AND** bulk insert all records via SqlBulkCopy from a DataTable
- **AND** return the count of staged records

### Requirement: Bulk repository performs MERGE upsert
The bulk repository SHALL execute a SQL MERGE statement from ##TempProducts into the Products table, inserting new records and updating changed ones.

#### Scenario: Upsert mix of new and changed products
- **WHEN** the MERGE statement is executed
- **THEN** records with matching ExternalId SHALL have Name, CategoryCode, Price, StockQuantity, IsActive, and UpdatedAtUtc updated if any value differs
- **AND** records with no matching ExternalId SHALL be inserted with CreatedAtUtc and UpdatedAtUtc set to current UTC
- **AND** the repository SHALL return counts of inserted and updated rows

### Requirement: Bulk repository performs soft delete cleanup
The bulk repository SHALL mark IsDeleted = 1 for all Products records whose ExternalId is not present in ##TempProducts.

#### Scenario: Products removed from source API
- **WHEN** the cleanup stage executes after MERGE
- **THEN** any product in Products with no matching ExternalId in ##TempProducts SHALL have IsDeleted set to 1
- **AND** UpdatedAtUtc SHALL be set to current UTC
- **AND** the repository SHALL return the count of soft-deleted records

### Requirement: Bulk repository drops temp table after sync
The bulk repository SHALL drop ##TempProducts after the sync completes, regardless of success or failure.

#### Scenario: Cleanup after successful sync
- **WHEN** the sync pipeline finishes
- **THEN** the repository SHALL execute DROP TABLE IF EXISTS ##TempProducts
- **AND** this cleanup SHALL run even if an earlier stage threw an exception

