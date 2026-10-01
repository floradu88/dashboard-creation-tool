# Confluence JSON input

Supporting account plans, RFP/RFI documents and request context, represented by scoped metadata and links.

**Loader status:** The mock API loads this version-1 contract from `src/CustomerDashboard.Infrastructure/MockData` at startup, without provider credentials. Invalid files fail startup. Read [the shared input contract](README.md) first.

## File and discovery

Save as `src/CustomerDashboard.Infrastructure/MockData/confluence.json`. Exact source discriminator: `Confluence`. Use `schemaVersion: 1` and `mode: "Mock"`. The API reads it at startup in Mock mode; restart after editing until explicit reload support is implemented. [Ready-to-edit example](../../src/CustomerDashboard.Infrastructure/MockData/confluence.json)

## Fields

Every field in the example is required unless its nullable behavior is stated here or in the shared contract. Empty collections use `[]`; use JSON numbers/booleans, not quoted strings. Shared envelope fields are defined in the input contract.

| Field | Type / meaning / constraints |
| --- | --- |
| data.pages[] | Required array of normalized page metadata. |
| pages[].id, customerId, spaceKey | Page ID, canonical customer ID, approved space from the customer mapping. |
| title, kind, version | Title; kind AccountPlan/RFP/RFI/Request/Contract/Other; positive integer page version. |
| updatedAtUtc, canonicalRequestIds, sourceUrl, summary | Document update time, linked canonical request IDs, original URL, short permitted summary. Request array may be empty. |

## Complete valid example

```json
{
  "schemaVersion": 1,
  "source": "Confluence",
  "mode": "Mock",
  "lastSuccessfulExtractionAtUtc": "2026-09-29T08:00:00Z",
  "dataThroughUtc": "2026-09-29T07:55:00Z",
  "data": {
    "pages": [
      {
        "id": "page-001",
        "customerId": "customer-001",
        "spaceKey": "NSDEMO",
        "title": "Account success plan",
        "kind": "AccountPlan",
        "version": 3,
        "updatedAtUtc": "2026-09-24T12:00:00Z",
        "canonicalRequestIds": [
          "request-001"
        ],
        "sourceUrl": "https://confluence.example/spaces/NSDEMO/pages/page-001",
        "summary": "Fictional account objectives and expansion requirements."
      }
    ]
  }
}
```

## Validation and interpretation

Page updatedAtUtc is not extraction time. AccountPlan pages supplement Salesforce account fields; they do not replace authoritative revenue or renewal values automatically. Do not parse arbitrary page prose into financial metrics. Use metadata and short synthetic summaries for mocks; live ingestion must respect page permissions and display rights.

This is a minimal, cross-linked example, not the full three-customer/twelve-month demonstration dataset. Add records inside the existing collections, preserve IDs and metadata semantics, and keep fixtures fictional. Malformed inputs fail startup with a file and field error; they are not served as zero-valued dashboard cards. The runnable demonstration set is `src/CustomerDashboard.Infrastructure/MockData`.

