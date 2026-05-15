## ADDED Requirements

### Requirement: Typed HttpClient consumes paginated external API
The external API client SHALL consume a paginated REST API and retrieve all pages until the full dataset is fetched.

#### Scenario: Fetch all pages from paginated API
- **WHEN** the sync job requests data from the external API
- **THEN** the client SHALL call the API page by page using the configured page size
- **AND** aggregate all items into a single collection
- **AND** stop when the total count is reached or a page returns empty

#### Scenario: Retry transient HTTP failures
- **WHEN** an HTTP request returns a transient error (5xx, timeout, network failure)
- **THEN** the client SHALL retry up to 3 times with exponential backoff (2s, 4s, 8s) and jitter
- **AND** if retries are exhausted, the circuit breaker SHALL open

### Requirement: Circuit breaker protects downstream API
The external API client SHALL use a Polly circuit breaker that opens after 5 consecutive failures and remains open for 30 seconds.

#### Scenario: Circuit breaker opens on repeated failures
- **WHEN** 5 consecutive HTTP calls fail
- **THEN** the circuit breaker SHALL open
- **AND** subsequent calls SHALL fail fast with a BrokenCircuitException
- **AND** after 30 seconds, the circuit SHALL transition to half-open and allow a trial call
