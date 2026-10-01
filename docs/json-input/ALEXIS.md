# Alexis JSON input

Recognized revenue, usage, SLA facts and delivery cost inputs. This is an application-owned mock contract; it does not claim to match the unknown Alexis database schema.

**Loader status:** The mock API loads this version-1 contract from `src/CustomerDashboard.Infrastructure/MockData` at startup, without provider credentials. Invalid files fail startup. Read [the shared input contract](README.md) first.

## File and discovery

Save as `src/CustomerDashboard.Infrastructure/MockData/alexis.json`. Exact source discriminator: `Alexis`. Use `schemaVersion: 1` and `mode: "Mock"`. The API reads it at startup in Mock mode; restart after editing until explicit reload support is implemented. [Ready-to-edit example](../../src/CustomerDashboard.Infrastructure/MockData/alexis.json)

## Fields

Every field in the example is required unless its nullable behavior is stated here or in the shared contract. Empty collections use `[]`; use JSON numbers/booleans, not quoted strings. Shared envelope fields are defined in the input contract.

| Field | Type / meaning / constraints |
| --- | --- |
| data.revenueEntries[], usageRecords[], slaObjectives[], slaEvents[], laborRates[], externalCosts[] | Required collections; all may be empty. |
| revenueEntries[] | id,customerId,recognitionDate,amount,category (Recurring/OneOff/Adjustment),description. Negative amounts allowed only for Adjustment. |
| usageRecords[] | id,customerId,periodStart,periodEndExclusive,featureId,unit,quantity,entitlement,activeUserIds. quantity >= 0; entitlement is nullable and nonnegative. User IDs are fictional stable IDs; deduplicate for active-user totals. |
| slaObjectives[] | id,customerId,name,kind (ResponseTime/ResolutionTime),thresholdMinutes >= 0,targetCompliancePercent from 0–100. |
| slaEvents[] | id,customerId,objectiveId,occurredAtUtc,actualMinutes >= 0,eligible,exclusionReason. Exclusion reason is required text when eligible=false; otherwise null. |
| laborRates[] | id,workerId,validFrom,validToExclusive,hourlyRate. Nonnegative rate; effective periods for a worker cannot overlap. Rates have no customerId because shared workers can serve multiple customers. |
| externalCosts[] | id,customerId,featureId,incurredDate,amount,description. Nonnegative already-attributed cost; do not reallocate it again. |

## Complete valid example

```json
{
  "schemaVersion": 1,
  "source": "Alexis",
  "mode": "Mock",
  "lastSuccessfulExtractionAtUtc": "2026-09-29T08:00:00Z",
  "dataThroughUtc": "2026-09-29T07:55:00Z",
  "data": {
    "revenueEntries": [
      {
        "id": "rev-001",
        "customerId": "customer-001",
        "recognitionDate": "2026-09-01",
        "amount": {
          "amount": 10000,
          "currency": "EUR"
        },
        "category": "Recurring",
        "description": "September recognized service revenue"
      }
    ],
    "usageRecords": [
      {
        "id": "usage-001",
        "customerId": "customer-001",
        "periodStart": "2026-09-01",
        "periodEndExclusive": "2026-10-01",
        "featureId": "feature-analytics",
        "unit": "ApiCall",
        "quantity": 125000,
        "entitlement": 200000,
        "activeUserIds": [
          "user-demo-01",
          "user-demo-02"
        ]
      }
    ],
    "slaObjectives": [
      {
        "id": "sla-response",
        "customerId": "customer-001",
        "name": "Priority 1 response",
        "kind": "ResponseTime",
        "thresholdMinutes": 60,
        "targetCompliancePercent": 99.5
      }
    ],
    "slaEvents": [
      {
        "id": "sla-event-001",
        "customerId": "customer-001",
        "objectiveId": "sla-response",
        "occurredAtUtc": "2026-09-20T10:00:00Z",
        "actualMinutes": 45,
        "eligible": true,
        "exclusionReason": null
      }
    ],
    "laborRates": [
      {
        "id": "rate-001",
        "workerId": "worker-001",
        "validFrom": "2026-01-01",
        "validToExclusive": "2027-01-01",
        "hourlyRate": {
          "amount": 80,
          "currency": "EUR"
        }
      }
    ],
    "externalCosts": [
      {
        "id": "cost-001",
        "customerId": "customer-001",
        "featureId": "feature-analytics",
        "incurredDate": "2026-09-21",
        "amount": {
          "amount": 250,
          "currency": "EUR"
        },
        "description": "Approved test environment cost"
      }
    ]
  }
}
```

## Validation and interpretation

An eligible SLA event meets its objective when actualMinutes <= thresholdMinutes. The sample gives 100% compliance from one eligible event, not a statistically robust service claim. Availability/downtime records are a future explicit schema extension, not fabricated from response-time events. Rates join to Jira workerId using the worklog date in the customer's reporting timezone. Usage periods are aggregate buckets; do not prorate partially overlapping buckets silently—require aligned query boundaries or mark coverage partial. Summing quantity is valid only for identical units/nonoverlapping buckets; active users are a union, not a sum. This sample yields EUR 10,000 September revenue, 62.5% entitlement consumption, and two active users.

This is a minimal, cross-linked example, not the full three-customer/twelve-month demonstration dataset. Add records inside the existing collections, preserve IDs and metadata semantics, and keep fixtures fictional. Malformed inputs fail startup with a file and field error; they are not served as zero-valued dashboard cards. The runnable demonstration set is `src/CustomerDashboard.Infrastructure/MockData`.

