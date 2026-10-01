# Slack JSON input

Relevant customer communication and request context. Messages supplement structured records rather than automatically becoming financial facts or new requests.

**Loader status:** The mock API loads this version-1 contract from `src/CustomerDashboard.Infrastructure/MockData` at startup, without provider credentials. Invalid files fail startup. Read [the shared input contract](README.md) first.

## File and discovery

Save as `src/CustomerDashboard.Infrastructure/MockData/slack.json`. Exact source discriminator: `Slack`. Use `schemaVersion: 1` and `mode: "Mock"`. The API reads it at startup in Mock mode; restart after editing until explicit reload support is implemented. [Ready-to-edit example](../../src/CustomerDashboard.Infrastructure/MockData/slack.json)

## Fields

Every field in the example is required unless its nullable behavior is stated here or in the shared contract. Empty collections use `[]`; use JSON numbers/booleans, not quoted strings. Shared envelope fields are defined in the input contract.

| Field | Type / meaning / constraints |
| --- | --- |
| data.channels[], messages[] | Required arrays. |
| channels[] | id,customerId,name. IDs must appear in the customer's approved slackChannelIds mapping. |
| messages[] | id,customerId,channelId,sentAtUtc,threadRootId,authorDisplayName,text,canonicalRequestIds,sourceUrl. threadRootId may be null; canonicalRequestIds may be empty. |

## Complete valid example

```json
{
  "schemaVersion": 1,
  "source": "Slack",
  "mode": "Mock",
  "lastSuccessfulExtractionAtUtc": "2026-09-29T08:00:00Z",
  "dataThroughUtc": "2026-09-29T07:55:00Z",
  "data": {
    "channels": [
      {
        "id": "slack-channel-001",
        "customerId": "customer-001",
        "name": "northstar-demo-account"
      }
    ],
    "messages": [
      {
        "id": "slack-message-001",
        "customerId": "customer-001",
        "channelId": "slack-channel-001",
        "sentAtUtc": "2026-09-25T10:00:00Z",
        "threadRootId": null,
        "authorDisplayName": "Demo Account Team",
        "text": "Fictional note: review analytics expansion requirements.",
        "canonicalRequestIds": [
          "request-001"
        ],
        "sourceUrl": "https://slack.example/archives/slack-channel-001/p001"
      }
    ]
  }
}
```

## Validation and interpretation

Each message must reference an existing mapped channel for the same customer. A non-null threadRootId refers to a message included in this bundle and channel. Stable message IDs represent source identity, not a new GUID on every extraction. Use only fictional text/people. Do not derive revenue, SLA breach status or sentiment from message text. Live Slack integration will map real channel/timestamp identifiers to this normalized contract.

This is a minimal, cross-linked example, not the full three-customer/twelve-month demonstration dataset. Add records inside the existing collections, preserve IDs and metadata semantics, and keep fixtures fictional. Malformed inputs fail startup with a file and field error; they are not served as zero-valued dashboard cards. The runnable demonstration set is `src/CustomerDashboard.Infrastructure/MockData`.

