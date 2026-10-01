# Future customer AI assistant

Status: not implemented. The loopback MCP tools in [MCP.md](MCP.md) let external chat agents call the mock queries. This document is the later in-app assistant, which is still deferred and does not send customer data to a model.

## User experience

Add an accessible chat panel available from every dashboard tab, scoped to the selected customer and reporting period. Display the current scope prominently. Start with suggested questions such as:

- What changed for this account during the selected period?
- What revenue is recognized, and what is still in the pipeline?
- Which requests are overdue, and which SLA objectives were breached?
- What did the analytics feature cost, and which inputs are missing?
- What renewals, risks and next actions should the account manager review?
- What recent customer news or market changes are relevant to this account?

Provide concise answers with links to the original records/dashboard sections, extraction dates and missing-data notes. Support follow-up questions within the same scope, a new-conversation action, cancellation and clear loading/error states. Switching customers starts a fresh scoped conversation; never reuse another customer's retrieved context silently.

## Grounded answers before predictions

1. Implement deterministic FAQ intents over the existing query handlers, so known financial/usage formulas are executed by application code.
2. Introduce a provider-neutral `IAssistantService` and a small set of typed, read-only tools for approved customer queries. Choose an AI provider only after privacy, hosting, model quality, cost and data-processing terms are agreed.
3. Retrieve authorized account facts and permitted document/news excerpts. The model explains tool results; it does not invent missing metrics or recalculate financial totals from prose.
4. Every factual answer carries source record IDs/links and extraction times. Distinguish facts, summaries, assumptions and suggestions. If evidence is missing, stale or contradictory, say so and link the conflicting sources.
5. Add response streaming only after basic request cancellation, bounded context, error handling and access control work.

Proposed future endpoints: `POST /api/v1/customers/{id}/assistant/conversations` and `POST /api/v1/customers/{id}/assistant/conversations/{conversationId}/messages`. Authorize both the customer and conversation on every request. Conversation persistence is optional initially; if added, define retention/deletion and restrict stored source content.

## Predictions and scenarios

Candidate use cases: usage trend, expected commercial outcomes, renewal risk, service demand and delivery-cost scenarios. Start with a transparent baseline (for example seasonal naive or a documented statistical forecast), then evaluate whether more complex methods improve performance. An LLM can explain the forecast and assumptions; it must not be the unvalidated calculator producing precise financial forecasts.

For each prediction, show target metric, as-of date, horizon, model/method version, input coverage, assumptions, validation error and an uncertainty interval when supported. Keep scenario inputs editable without presenting the scenario as an observed fact. Do not present weighted pipeline as recognized revenue or as an independently validated revenue forecast.

Minimum gates: sufficient relevant history, approved business definition, temporal holdout/backtesting, comparison to a simple baseline, documented missing-data behavior and no future-data leakage. The current single-customer fixture examples cannot validate prediction quality. Renewal-risk classification requires meaningful outcome labels and calibration before it can be described as reliable.

## Public information, later

Use approved news/market APIs and company disclosures through backend retrieval. Keep publisher, URL, publication/event date, retrieval time, permitted excerpt and customer identity match. Respect licensing/display rights and data freshness. Reconcile public company aliases/domains before linking information to the account. External signals may inform a scenario, but do not automatically overwrite internal financial records or prove causation.

Retrieved pages, Slack messages and documents are untrusted data. Ignore embedded instructions, limit retrieval to approved sources, and prevent model-directed arbitrary network requests. Keep credentials and hidden internal context out of prompts returned to users. Citation links must refer to retrieved records, not model-invented URLs.

## Controls and evaluation

- Enforce customer/role restrictions on retrieval and tools; UI visibility is not authorization. Cost/contact restrictions also apply to chat.
- Use allowlisted read-only tools with validated inputs. No CRM writes, Slack messages or autonomous actions in the first assistant release.
- Define provider budgets, request/token limits, timeouts and sanitized telemetry. Do not log raw conversations or customer documents by default.
- Evaluate FAQs, citation correctness, answer faithfulness, stale/missing data, unsupported predictions, cross-customer access and malicious source instructions.
- Include human review of representative account-management answers before rollout. Record feedback without silently treating user feedback as verified ground truth.

## Delivery order and acceptance

1. Curated FAQ + deterministic query tools on mocks: reproduce known answers and abstain where data is absent.
2. Grounded AI summaries on authorized snapshots: citations resolve, financial values match query results, and isolation tests pass.
3. Approved public-source retrieval: correct company matching, provenance, licensing and injection handling.
4. Evaluated forecasts/scenarios: demonstrate held-out performance, uncertainty and documented limitations before customer-facing use.

New infrastructure/projects should be introduced only when implementing these phases. Keep the assistant orchestration in Application and provider adapters in Infrastructure, with controllers as HTTP boundaries.
