# Customer intelligence dashboard

Local mock of the customer dashboard in [PLAN.md](PLAN.md). The API is a .NET 10 modular monolith and the UI is Angular 22. Both read fictional JSON. Live Salesforce, Alexis, Jira, Confluence, Slack, news, and market connections are not part of this phase.

```powershell
pwsh -File scripts/setup.ps1
pwsh -File scripts/run.ps1
```

Then open http://localhost:4200. The API is at http://127.0.0.1:5080, including OpenAPI, `/health/ready`, and the loopback MCP server at `/mcp`.

- [docs/RUNNING.md](docs/RUNNING.md) — prerequisites, ports, tests, and fixtures
- [docs/DATA-DICTIONARY.md](docs/DATA-DICTIONARY.md) — measured states and metric formulas
- [docs/MCP.md](docs/MCP.md) — connecting Claude, ChatGPT, Cursor, or another MCP client
- [docs/INTEGRATIONS.md](docs/INTEGRATIONS.md) — what mock mode does and does not call
- [src/CustomerDashboard.Infrastructure/MockData/README.md](src/CustomerDashboard.Infrastructure/MockData/README.md) — JSON format for adding a customer, and how to validate it
- [docs/json-input/README.md](docs/json-input/README.md) — normalized fixture contracts
