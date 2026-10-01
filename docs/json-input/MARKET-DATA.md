# Market data JSON input

Industry context and comparable benchmarks for account managers. This interprets the requested 'marked data' as market data.

**Loader status:** The mock API loads this version-1 contract from `src/CustomerDashboard.Infrastructure/MockData` at startup, without provider credentials. Invalid files fail startup. Read [the shared input contract](README.md) first.

## File and discovery

Save as `src/CustomerDashboard.Infrastructure/MockData/market-data.json`. Exact source discriminator: `MarketData`. Use `schemaVersion: 1` and `mode: "Mock"`. The API reads it at startup in Mock mode; restart after editing until explicit reload support is implemented. [Ready-to-edit example](../../src/CustomerDashboard.Infrastructure/MockData/market-data.json)

## Fields

Every field in the example is required unless its nullable behavior is stated here or in the shared contract. Empty collections use `[]`; use JSON numbers/booleans, not quoted strings. Shared envelope fields are defined in the input contract.

| Field | Type / meaning / constraints |
| --- | --- |
| data.indicators[] | Required array. |
| id, customerId, name, kind, provider | Stable observation ID, customer relevance, display name, kind Benchmark/IndustryIndicator/MarketIndicator, source provider. |
| sector, geography, metricId, definitionVersion | Comparison scope and a registered metric definition; never compare unknown metric definitions as equivalent. |
| periodStart, periodEndExclusive, observedAtUtc | Observation period and provider as-of timestamp. |
| value, unit, currency | value is number or null; unit Percent/Ratio/Count/Index/Currency. currency is ISO code for Currency and otherwise null. Percent uses 0–100 percentage points unless the named metric definition permits negative growth. |
| methodology, sampleSize, sourceUrl, isSimulated | Method description, nullable nonnegative integer sample size, original link, and true for mock observations. |

## Complete valid example

```json
{
  "schemaVersion": 1,
  "source": "MarketData",
  "mode": "Mock",
  "lastSuccessfulExtractionAtUtc": "2026-09-29T08:00:00Z",
  "dataThroughUtc": "2026-09-29T07:55:00Z",
  "data": {
    "indicators": [
      {
        "id": "market-001",
        "customerId": "customer-001",
        "name": "Sector usage growth benchmark",
        "kind": "Benchmark",
        "provider": "Demo Industry Research",
        "sector": "Software",
        "geography": "EU",
        "metricId": "usage-growth",
        "definitionVersion": 1,
        "periodStart": "2026-09-01",
        "periodEndExclusive": "2026-10-01",
        "observedAtUtc": "2026-09-28T12:00:00Z",
        "value": 12.5,
        "unit": "Percent",
        "currency": null,
        "methodology": "Fictional year-over-year usage growth for a comparable software cohort.",
        "sampleSize": 100,
        "sourceUrl": "https://research.example/software-usage-growth",
        "isSimulated": true
      }
    ]
  }
}
```

## Validation and interpretation

This example is 12.5 percent year-over-year usage growth, not a multiplier of 12.5. Its comparison period/methodology must match the customer's derived growth before a delta is shown. Missing baseline or mismatched cohort must display unavailable/incomparable. Synthetic figures are not actual current market conditions. Live provider choice, licensing and geography coverage remain open.

This is a minimal, cross-linked example, not the full three-customer/twelve-month demonstration dataset. Add records inside the existing collections, preserve IDs and metadata semantics, and keep fixtures fictional. Malformed inputs fail startup with a file and field error; they are not served as zero-valued dashboard cards. The runnable demonstration set is `src/CustomerDashboard.Infrastructure/MockData`.

