# Jira JSON input

Customer-linked features, work status, request links and approved hours for feature costs.

**Loader status:** The mock API loads this version-1 contract from `src/CustomerDashboard.Infrastructure/MockData` at startup, without provider credentials. Invalid files fail startup. Read [the shared input contract](README.md) first.

## File and discovery

Save as `src/CustomerDashboard.Infrastructure/MockData/jira.json`. Exact source discriminator: `Jira`. Use `schemaVersion: 1` and `mode: "Mock"`. The API reads it at startup in Mock mode; restart after editing until explicit reload support is implemented. [Ready-to-edit example](../../src/CustomerDashboard.Infrastructure/MockData/jira.json)

## Fields

Every field in the example is required unless its nullable behavior is stated here or in the shared contract. Empty collections use `[]`; use JSON numbers/booleans, not quoted strings. Shared envelope fields are defined in the input contract.

| Field | Type / meaning / constraints |
| --- | --- |
| data.features[], issues[], worklogs[] | Required arrays. |
| features[] | id,customerId,name. IDs must match usage and cost feature references. |
| issues[] | id,key,customerId,featureId,canonicalRequestId,type (Epic/Feature/Improvement/Bug/Task),title,status (ToDo/InProgress/Blocked/Done/Cancelled),createdAtUtc,updatedAtUtc,resolvedAtUtc,sourceUrl. featureId and canonicalRequestId may be null. resolvedAtUtc is null for open work. |
| worklogs[] | id,issueId,workerId,startedAtUtc,hours >= 0,approved,allocations[]. |
| allocations[] | customerId,featureId,fraction. Each fraction is > 0 and <= 1; fractions for a worklog sum to 1. Referenced customer/feature must exist. |

## Complete valid example

```json
{
  "schemaVersion": 1,
  "source": "Jira",
  "mode": "Mock",
  "lastSuccessfulExtractionAtUtc": "2026-09-29T08:00:00Z",
  "dataThroughUtc": "2026-09-29T07:55:00Z",
  "data": {
    "features": [
      {
        "id": "feature-analytics",
        "customerId": "customer-001",
        "name": "Analytics export improvement"
      }
    ],
    "issues": [
      {
        "id": "jira-001",
        "key": "NS-101",
        "customerId": "customer-001",
        "featureId": "feature-analytics",
        "canonicalRequestId": "request-001",
        "type": "Improvement",
        "title": "Add analytics export",
        "status": "InProgress",
        "createdAtUtc": "2026-09-12T09:00:00Z",
        "updatedAtUtc": "2026-09-25T10:00:00Z",
        "resolvedAtUtc": null,
        "sourceUrl": "https://jira.example/browse/NS-101"
      }
    ],
    "worklogs": [
      {
        "id": "worklog-001",
        "issueId": "jira-001",
        "workerId": "worker-001",
        "startedAtUtc": "2026-09-21T09:00:00Z",
        "hours": 5,
        "approved": true,
        "allocations": [
          {
            "customerId": "customer-001",
            "featureId": "feature-analytics",
            "fraction": 1
          }
        ]
      }
    ]
  }
}
```

## Validation and interpretation

Join approved hours to effective Alexis labor rates, then multiply by the customer/feature allocation. Do not count unapproved hours as actual cost. Preserve hours with missing rates and report incomplete cost coverage. The sample costs EUR 400 labor (5 × 80) plus EUR 250 external cost = EUR 650 for feature-analytics. Shared work uses explicit allocations; do not apply the full cost to every linked customer. Resolved dates must not precede created dates; status mappings will be implemented per live Jira workflow later.

This is a minimal, cross-linked example, not the full three-customer/twelve-month demonstration dataset. Add records inside the existing collections, preserve IDs and metadata semantics, and keep fixtures fictional. Malformed inputs fail startup with a file and field error; they are not served as zero-valued dashboard cards. The runnable demonstration set is `src/CustomerDashboard.Infrastructure/MockData`.

