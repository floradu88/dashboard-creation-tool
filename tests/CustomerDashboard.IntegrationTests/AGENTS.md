# Integration tests instructions

Scope: this directory and descendants. Read root and ancestor AGENTS.md files first.

Add with persistence and real adapters. Exercise pagination, throttling, checkpoints, retries, duplicate ingestion, revoked/deleted records and failed-snapshot preservation using controlled endpoints and isolated stores. Live smoke tests are explicit opt-in and must not depend on committed credentials.

Follow PLAN.md for contracts and milestone boundaries. These folders currently establish structure/instructions; inspect actual implementation before claiming anything is runnable.

