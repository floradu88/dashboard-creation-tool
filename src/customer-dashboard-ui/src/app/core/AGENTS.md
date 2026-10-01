# Angular core instructions

Scope: this directory and descendants. Read root and ancestor AGENTS.md files first.

Own API transport, configuration, routing infrastructure and later authentication. Keep source credentials out of the UI. Propagate API errors without raw stack traces. Cancel or ignore outdated customer/filter requests. Use the dev proxy and support configured ports. Do not put feature-specific business logic in interceptors or global services.

Follow PLAN.md for contracts and milestone boundaries. These folders currently establish structure/instructions; inspect actual implementation before claiming anything is runnable.

