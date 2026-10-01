# Application instructions

Scope: this directory and descendants. Read root and ancestor AGENTS.md files first.

Own typed DTOs, query handlers, narrow read ports and ingestion use cases. Reference Domain only. Avoid HTTP, EF Core, Hangfire and provider SDK dependencies. Keep provenance/coverage in results. Use directly injected handlers until a mediator is justified. Add commands only for actual writes; sync acceptance is not completion.

Follow PLAN.md for contracts and milestone boundaries. These folders currently establish structure/instructions; inspect actual implementation before claiming anything is runnable.

