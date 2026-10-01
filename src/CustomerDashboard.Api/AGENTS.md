# API instructions

Scope: this directory and descendants. Read root and ancestor AGENTS.md files first.

Use net10.0 controllers under /api/v1. Keep controllers thin and inject application handlers. Validate inputs, bound pagination, propagate cancellation, return Problem Details and publish OpenAPI. Customer authorization is server-side before reads/syncs when real data is enabled. Compose infrastructure only at startup; never calculate business metrics in controllers.

Follow PLAN.md for contracts and milestone boundaries. These folders currently establish structure/instructions; inspect actual implementation before claiming anything is runnable.

