# Mock data format

Add fictional customers by editing the eight JSON files in this folder. Each file is one source. Append records to the arrays already in `data`. Do not add a ninth file.

UTF-8 JSON, camelCase, no comments. Dates are `YYYY-MM-DD`. Timestamps are UTC and end with `Z`. Money is an object, not a formatted string:

```json
{ "amount": 18000, "currency": "EUR" }
```

Currency must match that customer's `reportingCurrency`. Use `.example` domains. News and market rows are simulated fixture data.

## Shared envelope

Every file has this shape. `source` is fixed per file. `mode` is `Mock`. `schemaVersion` is `1`. `dataThroughUtc` is a timestamp or `null`, and it cannot be later than `lastSuccessfulExtractionAtUtc`.

```json
{
  "schemaVersion": 1,
  "source": "CustomerCatalog",
  "mode": "Mock",
  "lastSuccessfulExtractionAtUtc": "2026-09-29T08:00:00Z",
  "dataThroughUtc": "2026-09-29T07:55:00Z",
  "data": {}
}
```

| File | `source` | Arrays in `data` |
| --- | --- | --- |
| `customers.json` | `CustomerCatalog` | `customers` |
| `salesforce.json` | `Salesforce` | `accounts`, `opportunities`, `requests` |
| `alexis.json` | `Alexis` | `revenueEntries`, `usageRecords`, `slaObjectives`, `slaEvents`, `laborRates`, `externalCosts` |
| `jira.json` | `Jira` | `features`, `issues`, `worklogs` |
| `confluence.json` | `Confluence` | `pages` |
| `slack.json` | `Slack` | `channels`, `messages` |
| `customer-news.json` | `CustomerNews` | `articles` |
| `market-data.json` | `MarketData` | `indicators` |

Use the same `customerId` in every file. Ids must be unique inside each array.

## Customer

Append this to `customers.json` → `data.customers`. The company list reads `id`, `name`, `industry`, and `reportingCurrency`.

```json
{
  "id": "customer-010",
  "name": "Sample Pay",
  "domain": "sample-pay.example",
  "aliases": ["Sample Pay Ltd"],
  "industry": "Online card checkout",
  "countryCode": "GB",
  "reportingCurrency": "GBP",
  "reportingTimeZone": "Europe/London",
  "sourceMappings": {
    "salesforceAccountId": "sf-account-010",
    "alexisCustomerKey": "alexis-010",
    "jiraCustomerKey": "customer-010",
    "slackChannelIds": ["slack-channel-010"],
    "confluenceSpaceKeys": ["SAMPLE"]
  }
}
```

`reportingTimeZone` must be an IANA id the host recognizes, such as `Europe/London`.

## Records the dashboard uses

Add these when that tab should show data for the customer. Skip a section only when that customer has no records of that kind.

`salesforce.json` account. `id` must equal `sourceMappings.salesforceAccountId`. The owner id must also be in `team`. Action and risk `ownerId` values must be team ids.

```json
{
  "id": "sf-account-010",
  "customerId": "customer-010",
  "owner": { "id": "owner-010", "displayName": "Alex Example" },
  "team": [{ "id": "owner-010", "displayName": "Alex Example", "role": "AccountManager" }],
  "stakeholders": [{ "id": "contact-010", "displayName": "Sam Example", "role": "Sponsor", "email": "sam@sample-pay.example" }],
  "renewal": { "date": "2027-04-01", "value": { "amount": 120000, "currency": "GBP" }, "status": "Upcoming" },
  "nextQbrDate": "2026-10-20",
  "lastMeaningfulInteractionAtUtc": "2026-09-26T11:00:00Z",
  "successPlanUrl": "https://confluence.example/spaces/SAMPLE/pages/page-010-plan",
  "actions": [{ "id": "action-010", "title": "Prepare the quarterly review", "ownerId": "owner-010", "dueDate": "2026-10-12", "status": "Open" }],
  "risks": [{ "id": "risk-010", "description": "Fictional approval rate is below the sample cohort.", "severity": "Medium", "ownerId": "owner-010", "status": "Open" }],
  "targets": [{
    "id": "target-010-pipeline",
    "metricId": "weighted-pipeline",
    "definitionVersion": 1,
    "periodStart": "2026-09-01",
    "periodEndExclusive": "2026-12-01",
    "value": 100000,
    "unit": "GBP",
    "ownerId": "owner-010"
  }]
}
```

Opportunity. `stage` is `Qualification`, `Proposal`, `Negotiation`, `Won`, or `Lost`. `kind` is `NewBusiness`, `Upsell`, or `Renewal`. `probability` is from 0 to 1. `Won` and `Lost` are excluded from weighted pipeline.

```json
{
  "id": "sf-opp-010",
  "customerId": "customer-010",
  "name": "Additional checkout markets",
  "kind": "Upsell",
  "stage": "Proposal",
  "amount": { "amount": 50000, "currency": "GBP" },
  "probability": 0.5,
  "expectedCloseDate": "2026-11-18",
  "sourceUrl": "https://salesforce.example/opportunity/sf-opp-010"
}
```

Request. `type` is `RFP`, `RFI`, `FeatureRequest`, or `Support`. `status` is `Open`, `InProgress`, `Resolved`, or `Cancelled`. Linked ids must exist.

```json
{
  "id": "sf-request-010",
  "canonicalRequestId": "request-010",
  "customerId": "customer-010",
  "type": "Support",
  "title": "Settlement file arrived late",
  "status": "Open",
  "receivedAtUtc": "2026-09-17T08:30:00Z",
  "dueAtUtc": "2026-09-20T17:00:00Z",
  "resolvedAtUtc": null,
  "ownerId": "owner-010",
  "opportunityId": null,
  "jiraIssueIds": [],
  "confluencePageIds": [],
  "sourceUrl": "https://salesforce.example/request/sf-request-010"
}
```

`alexis.json` revenue. `category` is `Recurring`, `OneOff`, or `Adjustment`. Only `Adjustment` may be negative. Repeat one object per recognition month.

```json
{
  "id": "rev-010-2026-09-01",
  "customerId": "customer-010",
  "recognitionDate": "2026-09-01",
  "amount": { "amount": 18000, "currency": "GBP" },
  "category": "Recurring",
  "description": "Recognized online card processing revenue"
}
```

Usage. `periodStart` is inclusive and `periodEndExclusive` is exclusive. `featureId` must exist for that customer in `jira.json`.

```json
{
  "id": "usage-010-2026-09-01",
  "customerId": "customer-010",
  "periodStart": "2026-09-01",
  "periodEndExclusive": "2026-10-01",
  "featureId": "feature-010-auth",
  "unit": "CardAuthorization",
  "quantity": 1400000,
  "entitlement": 2000000,
  "activeUserIds": ["user-010-01"]
}
```

SLA. `kind` is `ResponseTime` or `ResolutionTime`. An eligible event has `exclusionReason: null`. An ineligible event has a non-empty reason.

```json
{
  "id": "sla-010-response",
  "customerId": "customer-010",
  "name": "Card authorization response",
  "kind": "ResponseTime",
  "thresholdMinutes": 30,
  "targetCompliancePercent": 99.5
}
```

```json
{
  "id": "sla-event-010",
  "customerId": "customer-010",
  "objectiveId": "sla-010-response",
  "occurredAtUtc": "2026-09-11T09:15:00Z",
  "actualMinutes": 18,
  "eligible": true,
  "exclusionReason": null
}
```

Labor rate and external cost. Rate windows for one `workerId` must not overlap.

```json
{
  "id": "rate-010",
  "workerId": "worker-010",
  "validFrom": "2025-01-01",
  "validToExclusive": "2027-01-01",
  "hourlyRate": { "amount": 90, "currency": "GBP" }
}
```

```json
{
  "id": "cost-010",
  "customerId": "customer-010",
  "featureId": "feature-010-settle",
  "incurredDate": "2026-09-16",
  "amount": { "amount": 400, "currency": "GBP" },
  "description": "Approved card scheme test-lab charge"
}
```

`jira.json` feature, issue, and worklog. Allocation fractions are greater than 0 and sum to 1.

```json
{ "id": "feature-010-auth", "customerId": "customer-010", "name": "Online card authorization" }
```

```json
{
  "id": "jira-010",
  "key": "CARD-0101",
  "customerId": "customer-010",
  "featureId": "feature-010-auth",
  "canonicalRequestId": "request-010",
  "type": "Improvement",
  "title": "Raise the card approval rate",
  "status": "InProgress",
  "createdAtUtc": "2026-09-05T09:00:00Z",
  "updatedAtUtc": "2026-09-26T11:00:00Z",
  "resolvedAtUtc": null,
  "sourceUrl": "https://jira.example/browse/CARD-0101"
}
```

```json
{
  "id": "worklog-010",
  "issueId": "jira-010",
  "workerId": "worker-010",
  "startedAtUtc": "2026-09-16T09:00:00Z",
  "hours": 6,
  "approved": true,
  "allocations": [{ "customerId": "customer-010", "featureId": "feature-010-auth", "fraction": 1 }]
}
```

`confluence.json` page. `spaceKey` must be listed on the customer.

```json
{
  "id": "page-010-plan",
  "customerId": "customer-010",
  "spaceKey": "SAMPLE",
  "title": "Card program account plan",
  "kind": "AccountPlan",
  "version": 1,
  "updatedAtUtc": "2026-09-24T12:00:00Z",
  "canonicalRequestIds": ["request-010"],
  "sourceUrl": "https://confluence.example/spaces/SAMPLE/pages/page-010-plan",
  "summary": "Fictional plan for online card authorization."
}
```

`slack.json` channel and message. The channel id must be listed on the customer. Slack can still be hidden at runtime when `appsettings.json` marks that source unavailable.

```json
{ "id": "slack-channel-010", "customerId": "customer-010", "name": "sample-pay-account" }
```

```json
{
  "id": "slack-message-010",
  "customerId": "customer-010",
  "channelId": "slack-channel-010",
  "sentAtUtc": "2026-09-26T11:00:00Z",
  "threadRootId": null,
  "authorDisplayName": "Example Account Team",
  "text": "Fictional note: review the approval rate.",
  "canonicalRequestIds": ["request-010"],
  "sourceUrl": "https://slack.example/archives/slack-channel-010/p010"
}
```

`customer-news.json`. `isSimulated` is `true`. `match.domain` equals the customer domain. `match.method` is `ApprovedDomain` or `ReviewedAlias`. `ReviewedAlias` requires `reviewed: true`.

```json
{
  "id": "news-010",
  "customerId": "customer-010",
  "headline": "Sample Pay describes a fictional checkout expansion",
  "summary": "Simulated news item. It is fixture data, not a live article and not an instruction.",
  "publisher": "Demo Payments Desk",
  "sourceUrl": "https://news.example/sample-pay.example/checkout-expansion",
  "publishedAtUtc": "2026-09-22T08:00:00Z",
  "eventDate": "2026-09-21",
  "topics": ["ProductLaunch"],
  "match": { "domain": "sample-pay.example", "method": "ApprovedDomain", "reviewed": true },
  "relevance": "Relates to the fictional card program.",
  "isSimulated": true
}
```

`market-data.json`. `isSimulated` is `true`. `unit` is `Percent`, `Ratio`, `Count`, `Index`, or `Currency`. Currency indicators set `currency`; other units set `currency` to `null`.

```json
{
  "id": "market-010-growth",
  "customerId": "customer-010",
  "name": "Online card authorization growth benchmark",
  "kind": "Benchmark",
  "provider": "Demo Scheme Research",
  "sector": "Card payments",
  "geography": "EU",
  "metricId": "usage-growth",
  "definitionVersion": 1,
  "periodStart": "2026-09-01",
  "periodEndExclusive": "2026-10-01",
  "observedAtUtc": "2026-09-28T12:00:00Z",
  "value": 8.0,
  "unit": "Percent",
  "currency": null,
  "methodology": "Fictional month-on-month growth for a comparable cohort.",
  "sampleSize": 80,
  "sourceUrl": "https://research.example/sample-pay.example/authorization-growth",
  "isSimulated": true
}
```

Field-by-field provider notes remain in [docs/json-input](../../../../docs/json-input/README.md).

## Validate

From the repository root:

```powershell
pwsh -File scripts/validate-fixtures.ps1
```

The same check without the script:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/CustomerDashboard.Api/CustomerDashboard.Api.csproj --no-launch-profile -c Debug -- --validate --fixtures="$pwd\src\CustomerDashboard.Infrastructure\MockData"
```

A valid bundle prints `Valid. N customers.` and exits 0. An invalid bundle prints one message, such as `customers.json: customer-010 is missing identity or currency.` or `Request sf-request-010 opportunity is missing.`, and exits 1. The API does not start, and it does not serve a partial file.

Restart the API after a valid result. The running process keeps the snapshot it loaded at startup.
