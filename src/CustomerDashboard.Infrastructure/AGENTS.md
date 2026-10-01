# Infrastructure instructions

Scope: this directory and descendants. Read root and ancestor AGENTS.md files first.

Implement Application ports. Start with JSON data services; add typed provider integrators and persistent snapshot readers later. Isolate Salesforce, Alexis, Jira, Confluence, Slack, news and market access. Do not assume Alexis schema/API. Use pagination, bounded retries, cancellation, idempotency and atomic snapshot publication. Never silently substitute mock data after live failure. Secrets remain server-side.

Follow PLAN.md for contracts and milestone boundaries. These folders currently establish structure/instructions; inspect actual implementation before claiming anything is runnable.

