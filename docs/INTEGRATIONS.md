# Integrations

Status: mock only. Milestones 4–6 in [PLAN.md](../PLAN.md) (database snapshots, live providers, and Hangfire) are not implemented. The API rejects any mode other than Mock and refuses to start outside Development or Testing.

| Source | Mock behavior |
| --- | --- |
| Customer catalog | `customers.json`. Canonical ids are `customer-001`, `customer-002`, and `customer-003`. |
| Salesforce | Accounts, opportunities, requests, targets, and actions. Stakeholder email addresses stay in the fixture and are omitted from API and MCP responses. |
| Alexis | Recognized revenue, usage, SLA events, labor rates, and external costs. |
| Jira | Features, issues, and worklogs. Shared hours use the fixture allocation fraction. |
| Confluence | Customer-space pages linked from requests. |
| Slack | Records are present, but `DataSources:Slack:Availability` is `Unavailable`. Those records are omitted and the source status is unavailable. |
| Customer news | Simulated articles. Duplicates with the same canonical URL collapse to the lowest id. Publisher, publication date, and extraction time stay separate. |
| Market data | Simulated indicators. A null benchmark stays missing. |

There is no upstream HTTP client in this phase. A live failure cannot fall back to these fixtures because live mode is not loaded. Changing a fixture requires an API restart; extraction timestamps come from the files, and `generatedAtUtc` is the response time.

The MCP endpoint in [MCP.md](MCP.md) is a read-only adapter over these same queries. It is not a provider integration.

Sync routes, OIDC, and the in-app assistant in [AI-ASSISTANT.md](AI-ASSISTANT.md) remain deferred. Before any real customer data, the server must authorize the customer and role on both REST and MCP.
