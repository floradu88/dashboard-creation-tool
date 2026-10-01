# Customer catalog JSON input

Required shared identity file for the customer selector and all provider joins. This is configuration/reference data rather than an external integration.

**Loader status:** The mock API loads this version-1 contract from `src/CustomerDashboard.Infrastructure/MockData` at startup, without provider credentials. Invalid files fail startup. Read [the shared input contract](README.md) first.

## File and discovery

Save as `src/CustomerDashboard.Infrastructure/MockData/customers.json`. Exact source discriminator: `CustomerCatalog`. Use `schemaVersion: 1` and `mode: "Mock"`. The API reads it at startup in Mock mode; restart after editing until explicit reload support is implemented. [Ready-to-edit example](../../src/CustomerDashboard.Infrastructure/MockData/customers.json)

## Fields

Every field in the example is required unless its nullable behavior is stated here or in the shared contract. Empty collections use `[]`; use JSON numbers/booleans, not quoted strings. Shared envelope fields are defined in the input contract.

| Field | Type / meaning / constraints |
| --- | --- |
| data.customers[] | Customer records; id must be globally unique and nonempty. |
| id, name, domain, aliases | Canonical customer ID, display name, approved domain, alternative company names. Never join solely on name. |
| industry, countryCode | Industry label and ISO two-letter country code. |
| reportingCurrency, reportingTimeZone | ISO three-letter currency and recognized IANA reporting timezone. |
| sourceMappings | Explicit external identity mappings. All listed keys required; a mapping not yet available may be null, and channel/space arrays may be empty. |

## Complete valid example

```json
{
  "schemaVersion": 1,
  "source": "CustomerCatalog",
  "mode": "Mock",
  "lastSuccessfulExtractionAtUtc": "2026-09-29T08:00:00Z",
  "dataThroughUtc": "2026-09-29T07:55:00Z",
  "data": {
    "customers": [
      {
        "id": "customer-001",
        "name": "Northstar Demo",
        "domain": "northstar.example",
        "aliases": [
          "Northstar Demo Ltd"
        ],
        "industry": "Software",
        "countryCode": "RO",
        "reportingCurrency": "EUR",
        "reportingTimeZone": "Europe/Bucharest",
        "sourceMappings": {
          "salesforceAccountId": "sf-account-001",
          "alexisCustomerKey": "alexis-001",
          "jiraCustomerKey": "customer-001",
          "slackChannelIds": [
            "slack-channel-001"
          ],
          "confluenceSpaceKeys": [
            "NSDEMO"
          ]
        }
      }
    ]
  }
}
```

## Validation and interpretation

Add a customer here before inserting that customerId into provider records. External account IDs must match Salesforce accounts. Slack channel IDs and Confluence spaces must match their files. CustomerNews and MarketData use canonical customerId plus the approved domain; they need no guessed external mapping.

This is a minimal, cross-linked example, not the full three-customer/twelve-month demonstration dataset. Add records inside the existing collections, preserve IDs and metadata semantics, and keep fixtures fictional. Malformed inputs fail startup with a file and field error; they are not served as zero-valued dashboard cards. The runnable demonstration set is `src/CustomerDashboard.Infrastructure/MockData`.

