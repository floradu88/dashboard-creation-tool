# Customer news JSON input

News cards about the selected customer. The actual feed vendor is not selected; these are normalized fictional articles.

**Loader status:** The mock API loads this version-1 contract from `src/CustomerDashboard.Infrastructure/MockData` at startup, without provider credentials. Invalid files fail startup. Read [the shared input contract](README.md) first.

## File and discovery

Save as `src/CustomerDashboard.Infrastructure/MockData/customer-news.json`. Exact source discriminator: `CustomerNews`. Use `schemaVersion: 1` and `mode: "Mock"`. The API reads it at startup in Mock mode; restart after editing until explicit reload support is implemented. [Ready-to-edit example](../../src/CustomerDashboard.Infrastructure/MockData/customer-news.json)

## Fields

Every field in the example is required unless its nullable behavior is stated here or in the shared contract. Empty collections use `[]`; use JSON numbers/booleans, not quoted strings. Shared envelope fields are defined in the input contract.

| Field | Type / meaning / constraints |
| --- | --- |
| data.articles[] | Required array. |
| id, customerId, headline, summary, publisher, sourceUrl | Stable article ID, canonical customer, headline, short permitted summary, publisher and canonical original link. |
| publishedAtUtc, eventDate | Publication timestamp and nullable actual event date; neither is extraction time. |
| topics, match, relevance, isSimulated | Topics array; match {domain,method,reviewed}; explanation of relevance; isSimulated must be true in these fixtures. |
| match.method | ApprovedDomain or ReviewedAlias. domain must match the catalog domain; ReviewedAlias requires reviewed=true. |

## Complete valid example

```json
{
  "schemaVersion": 1,
  "source": "CustomerNews",
  "mode": "Mock",
  "lastSuccessfulExtractionAtUtc": "2026-09-29T08:00:00Z",
  "dataThroughUtc": "2026-09-29T07:55:00Z",
  "data": {
    "articles": [
      {
        "id": "news-001",
        "customerId": "customer-001",
        "headline": "Northstar Demo announces a fictional product expansion",
        "summary": "Simulated news item for dashboard development.",
        "publisher": "Demo Business News",
        "sourceUrl": "https://news.example/northstar-demo-expansion",
        "publishedAtUtc": "2026-09-24T08:00:00Z",
        "eventDate": "2026-09-23",
        "topics": [
          "ProductLaunch"
        ],
        "match": {
          "domain": "northstar.example",
          "method": "ApprovedDomain",
          "reviewed": true
        },
        "relevance": "Relates to the customer's analytics expansion plans.",
        "isSimulated": true
      }
    ]
  }
}
```

## Validation and interpretation

Use publisher+canonical URL to deduplicate articles while preserving stable record IDs. Customer relevance must be supported by approved identity mapping, not a name-only fuzzy match. Keep exact dates and source links visible. The example URLs use reserved .example domains intentionally and are not real news destinations. Do not substitute invented stories about real companies.

This is a minimal, cross-linked example, not the full three-customer/twelve-month demonstration dataset. Add records inside the existing collections, preserve IDs and metadata semantics, and keep fixtures fictional. Malformed inputs fail startup with a file and field error; they are not served as zero-valued dashboard cards. The runnable demonstration set is `src/CustomerDashboard.Infrastructure/MockData`.

