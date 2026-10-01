# JSON input contracts

Use these guides to prepare JSON for the mocked dashboard. The supplied files are **application-owned normalized inputs**, not exports that exactly match Salesforce, Slack or other vendor API responses. Later integrators will map real records into these contracts.

**Add a customer:** the copy-paste JSON shape and the validate command are in [MockData/README.md](../../src/CustomerDashboard.Infrastructure/MockData/README.md). `pwsh -File scripts/validate-fixtures.ps1` runs the same checks the API uses at startup.

**Current state:** The mock API loads these eight files from `src/CustomerDashboard.Infrastructure/MockData` at startup. The files are the fictional demonstration set: the original three customers and their edge cases, plus six online card-transaction companies (Lumen Checkout, Nimbus Pay, Quay Ledger, Halcyon Wallet, Plexa Checkout, and Brindle Commerce) with records in every dashboard category. Saving a change appears after the API restarts. Invalid files fail startup and are not served.

## One guide per provider

| Guide | File | Supplies |
| --- | --- | --- |
| [Customers](CUSTOMERS.md) | customers.json | Required canonical customer catalog and source mappings |
| [Salesforce](SALESFORCE.md) | salesforce.json | Commercial, RFP/RFI, account management and metric targets |
| [Alexis](ALEXIS.md) | alexis.json | Revenue, usage, SLA events, labor rates and external costs |
| [Jira](JIRA.md) | jira.json | Work items, features and allocated worklogs |
| [Confluence](CONFLUENCE.md) | confluence.json | Plans and supporting request documents |
| [Slack](SLACK.md) | slack.json | Customer communication context |
| [Customer news](CUSTOMER-NEWS.md) | customer-news.json | Simulated customer news; live vendor to select |
| [Market data](MARKET-DATA.md) | market-data.json | Simulated sector indicators/benchmarks; live vendor to select |

Account management reuses Salesforce accounts and linked Confluence/Jira records; there is no separate account-management.json authority. Metric actuals are computed from provider records; targets live in Salesforce account fixtures. Do not create a separate precomputed metrics file that bypasses domain calculations.

## Planned pickup workflow

1. Put the files in `src/CustomerDashboard.Infrastructure/MockData/`; retain the exact filenames in the table. The loader uses an explicit manifest, not arbitrary wildcard discovery.
2. Edit `customers.json` first. Use its canonical ID in all customer-scoped provider records. Define external ID/channel/space mappings explicitly.
3. Edit the provider's collections using its guide. Keep `source`, `schemaVersion` and `mode` exact. Add multiple customers inside each provider file, rather than extra files with unrecognized names.
4. Configure these providers as Mock. Default `MockData:RootPath` resolves relative to the API content root. Copy fixture files into API build/publish output under `MockData/`; the Infrastructure project alone is not the runtime content root.
5. Start/restart the API using the run script once implemented. Validate the entire bundle before publishing a new in-memory snapshot. Missing required files or invalid references fail startup/readiness with sanitized file/field details. No partial invalid snapshot is served.
6. Open a customer tab and verify source/mode/extraction labels, values and date filters. Reloading a page must not change extraction timestamps.

Explicit per-source failure/latency scenarios should use development-only configuration, not malformed input JSON. Successful empty arrays mean no records; a failed source means unavailable. An external provider not configured for Mock is not loaded from these fixtures.

## Common envelope (all eight files)

| Field | Required type | Meaning |
| --- | --- | --- |
| schemaVersion | integer, exactly 1 | Application input schema version |
| source | string | Exact discriminator from the provider guide |
| mode | string, exactly Mock | Marks fictional input |
| lastSuccessfulExtractionAtUtc | UTC ISO-8601 timestamp ending Z | Fixed successful snapshot/extraction time; never response-generation time |
| dataThroughUtc | UTC timestamp or null | Known coverage watermark; null if unknown |
| data | object | Exact collections specified by the provider guide |

The sample extraction time is illustrative. Never label the samples current just because the API has restarted. `dataThroughUtc` cannot exceed extraction time. Future expected close/renewal/target dates are allowed; they do not claim coverage of future observed events.

## Shared field conventions

- JSON is UTF-8, camelCase, with no comments or trailing commas. Use exact property names and case-sensitive enums. The future validator should reject unknown fields to catch misspellings; changes require a deliberate schema-version decision.
- All properties shown in examples are required. The only allowed nulls are those specifically listed in each guide and `dataThroughUtc`. Strings are nonempty unless the contract explicitly says otherwise; arrays may be empty.
- Stable string IDs identify records. Uniqueness is within each named collection/source; canonical customer/request/feature IDs join across sources. Every referenced record must exist in this mock bundle, except provider URLs, external mapping strings with no imported records, and catalog references explicitly allowed to be null.
- Timestamps ending `AtUtc` use ISO-8601 UTC with Z. Calendar dates use YYYY-MM-DD. Reporting windows are `[periodStart, periodEndExclusive)`, with start before end. Do not confuse date-only reporting boundaries with UTC timestamps.
- Money uses `{ "amount": 10000, "currency": "EUR" }`. Backend deserializes amount as decimal. Do not use currency symbols, thousands separators or formatted strings. Currency must match customer reporting currency until explicit conversion is implemented.
- Numbers use a decimal point. Probabilities/allocation fractions are 0–1; percentage fields are percentage points. Examples specify which unit applies. JSON does not allow NaN or Infinity.
- Customer isolation and role restrictions remain application responsibilities; fixture IDs are not authorization. Keep only fictional customers, contacts, rates and content in checked-in fixtures.
- URLs are absolute HTTPS source links. Sample URLs intentionally use reserved `.example` domains. No secrets, access tokens or authenticated query strings belong in URLs.
- `scripts/validate-fixtures.ps1` loads these files through the API fixture store. It checks schema, source, mode, ids, currency, and cross-file references. JSON syntax alone does not prove the bundle is usable.

## Example cross-provider relationships

```text
customer-001 -> sf-account-001 / alexis-001 / slack-channel-001 / NSDEMO
request-001 -> sf-request-001 -> jira-001 -> page-001
feature-analytics -> usage-001 / jira-001 / cost-001
worklog-001 -> worker-001 -> rate-001
news-001 -> customer-001 + northstar.example
market-001 -> customer-001 + usage-growth definition version 1
```

The sample's September revenue is EUR 10,000. Its November weighted pipeline is EUR 18,000. Feature cost is EUR 650 (EUR 400 labor + EUR 250 external). One eligible SLA event meets its target. The market growth benchmark cannot be compared to customer growth without matching baseline usage; show the customer comparison as unavailable rather than inventing it.

## Before implementation is called complete

Add typed loading/validation, file publish-copy configuration, schema/version and cross-reference checks, date/currency validation, startup failure tests and stable provenance handling. Expand to three fictional customers and twelve months of history plus negative scenarios in separate test fixtures. Keep these Markdown examples and ready-to-edit JSON synchronized whenever the contract changes.

