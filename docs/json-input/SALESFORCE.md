# Salesforce JSON input

Commercial opportunities, RFP/RFI/request history, account ownership, renewals, stakeholders, account actions, risks and metric targets.

**Loader status:** The mock API loads this version-1 contract from `src/CustomerDashboard.Infrastructure/MockData` at startup, without provider credentials. Invalid files fail startup. Read [the shared input contract](README.md) first.

## File and discovery

Save as `src/CustomerDashboard.Infrastructure/MockData/salesforce.json`. Exact source discriminator: `Salesforce`. Use `schemaVersion: 1` and `mode: "Mock"`. The API reads it at startup in Mock mode; restart after editing until explicit reload support is implemented. [Ready-to-edit example](../../src/CustomerDashboard.Infrastructure/MockData/salesforce.json)

## Fields

Every field in the example is required unless its nullable behavior is stated here or in the shared contract. Empty collections use `[]`; use JSON numbers/booleans, not quoted strings. Shared envelope fields are defined in the input contract.

| Field | Type / meaning / constraints |
| --- | --- |
| data.accounts[], opportunities[], requests[] | Required arrays, even when empty. |
| accounts[].id, customerId | External account ID plus canonical customer ID, consistent with customers.json. |
| owner, team[], stakeholders[] | Owner/team IDs and display names; team role; stakeholder ID, displayName, role and email. Email is nullable. |
| renewal, nextQbrDate, lastMeaningfulInteractionAtUtc, successPlanUrl | Renewal {date,value,status}; all four account fields may be null when unknown. Renewal status: Upcoming, Negotiating, Renewed, Lost. |
| actions[], risks[] | Actions: id,title,ownerId,dueDate,status (Open/InProgress/Done). Risks: id,description,severity (Low/Medium/High),ownerId,status (Open/Mitigated/Closed). |
| targets[] | id,metricId,definitionVersion,periodStart,periodEndExclusive,value,unit,ownerId. Positive version; numeric target, same unit/period as compared metric. |
| opportunities[] | id,customerId,name,kind (NewBusiness/Upsell/Renewal),stage (Qualification/Proposal/Negotiation/Won/Lost),amount,probability,expectedCloseDate,sourceUrl. Probability is 0–1, not 0–100. |
| requests[] | id,canonicalRequestId,customerId,type (RFP/RFI/FeatureRequest/Support),title,status (Open/InProgress/Resolved/Cancelled),receivedAtUtc,dueAtUtc,resolvedAtUtc,ownerId,opportunityId,jiraIssueIds,confluencePageIds,sourceUrl. dueAtUtc,resolvedAtUtc,opportunityId may be null. |

## Complete valid example

```json
{
  "schemaVersion": 1,
  "source": "Salesforce",
  "mode": "Mock",
  "lastSuccessfulExtractionAtUtc": "2026-09-29T08:00:00Z",
  "dataThroughUtc": "2026-09-29T07:55:00Z",
  "data": {
    "accounts": [
      {
        "id": "sf-account-001",
        "customerId": "customer-001",
        "owner": {
          "id": "owner-001",
          "displayName": "Alex Demo"
        },
        "team": [
          {
            "id": "owner-001",
            "displayName": "Alex Demo",
            "role": "AccountManager"
          }
        ],
        "stakeholders": [
          {
            "id": "contact-001",
            "displayName": "Sam Example",
            "role": "Sponsor",
            "email": "sam@northstar.example"
          }
        ],
        "renewal": {
          "date": "2027-01-01",
          "value": {
            "amount": 120000,
            "currency": "EUR"
          },
          "status": "Upcoming"
        },
        "nextQbrDate": "2026-10-15",
        "lastMeaningfulInteractionAtUtc": "2026-09-25T10:00:00Z",
        "successPlanUrl": "https://confluence.example/spaces/NSDEMO/pages/page-001",
        "actions": [
          {
            "id": "action-001",
            "title": "Confirm adoption goals",
            "ownerId": "owner-001",
            "dueDate": "2026-10-02",
            "status": "Open"
          }
        ],
        "risks": [
          {
            "id": "risk-001",
            "description": "Low adoption in a new team",
            "severity": "Medium",
            "ownerId": "owner-001",
            "status": "Open"
          }
        ],
        "targets": [
          {
            "id": "target-001",
            "metricId": "weighted-pipeline",
            "definitionVersion": 1,
            "periodStart": "2026-10-01",
            "periodEndExclusive": "2027-01-01",
            "value": 50000,
            "unit": "EUR",
            "ownerId": "owner-001"
          }
        ]
      }
    ],
    "opportunities": [
      {
        "id": "sf-opp-001",
        "customerId": "customer-001",
        "name": "Additional team rollout",
        "kind": "Upsell",
        "stage": "Proposal",
        "amount": {
          "amount": 30000,
          "currency": "EUR"
        },
        "probability": 0.6,
        "expectedCloseDate": "2026-11-15",
        "sourceUrl": "https://salesforce.example/opportunity/sf-opp-001"
      }
    ],
    "requests": [
      {
        "id": "sf-request-001",
        "canonicalRequestId": "request-001",
        "customerId": "customer-001",
        "type": "RFP",
        "title": "Analytics expansion proposal",
        "status": "InProgress",
        "receivedAtUtc": "2026-09-10T09:00:00Z",
        "dueAtUtc": "2026-10-10T16:00:00Z",
        "resolvedAtUtc": null,
        "ownerId": "owner-001",
        "opportunityId": "sf-opp-001",
        "jiraIssueIds": [
          "jira-001"
        ],
        "confluencePageIds": [
          "page-001"
        ],
        "sourceUrl": "https://salesforce.example/request/sf-request-001"
      }
    ]
  }
}
```

## Validation and interpretation

ownerId must refer to an account team member. Cross-provider links must exist in the fixture bundle. Deduplicate request history using canonicalRequestId; Jira and Slack links are supporting context, not extra requests. The example weighted open pipeline is EUR 18,000 (30,000 × 0.6) for the close period. This is not September recognized revenue. Account targets are inputs, while metric actuals are derived by handlers.

This is a minimal, cross-linked example, not the full three-customer/twelve-month demonstration dataset. Add records inside the existing collections, preserve IDs and metadata semantics, and keep fixtures fictional. Malformed inputs fail startup with a file and field error; they are not served as zero-valued dashboard cards. The runnable demonstration set is `src/CustomerDashboard.Infrastructure/MockData`.

