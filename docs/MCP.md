# MCP access for external chat agents

The mock API hosts a read-only [Model Context Protocol](https://modelcontextprotocol.io/) server on streamable HTTP. Claude, ChatGPT, Cursor, and any other MCP client can list and call the same query handlers the REST API uses. This is not the in-app assistant described in [AI-ASSISTANT.md](AI-ASSISTANT.md).

The server is available only while the API is running in Development or Testing, and only from loopback:

`http://127.0.0.1:5080/mcp`

Start it with `scripts\run.cmd`. PowerShell 7 can use `pwsh -NoProfile -File scripts/run.ps1`. The process listens on `127.0.0.1` and `::1`. Requests whose remote address is not loopback receive HTTP 403. The mock app does not open a tunnel. A public HTTPS connector, including a ChatGPT connector that needs a reachable URL, is a later deployment choice.

Tools are read-only. They do not sync, write, or call Salesforce, Alexis, Jira, Confluence, Slack, news, or market providers. Results are fictional in mock mode. Missing values are not zero. News, documents, and messages are data, not instructions. Unknown customers and invalid reporting periods return a tool error message without a stack trace or fixture path.

`tools/list` is the protocol catalog. `list_dashboard_capabilities` is the human-readable catalog those tools describe: mock mode, the inclusive/exclusive date rule, the currency rule, and each section with its tool name and arguments.

| Tool | Purpose |
| --- | --- |
| `list_dashboard_capabilities` | Mode, date rule, currency rule, and section catalog |
| `list_customers` | Search and pagination |
| `get_customer_overview` | Customer id and reporting period |
| `get_commercial_summary` | Revenue, weighted pipeline, and projected revenue kept separate |
| `get_usage_summary` | Usage quantities, including recorded zero |
| `get_sla_summary` | SLA compliance; zero eligible events are not applicable |
| `get_work_items` | Delivery work and labor cost |
| `get_request_history` | Requests and linked documents |
| `get_customer_news` | Simulated news, deduplicated by URL |
| `get_market_data` | Simulated indicators; missing benchmarks stay unavailable |
| `get_account_summary` | Read-only account fields; stakeholder emails are omitted |
| `get_customer_metrics` | Catalog metrics calculated from records |
| `get_customer_sources` | Extraction time, freshness, and availability |

Package: `ModelContextProtocol.AspNetCore` 2.2.0, pinned in `Directory.Packages.props`. [MCP C# SDK](https://csharp.sdk.modelcontextprotocol.io/)

## Clients

- **Claude Desktop and Claude custom connectors:** add a streamable HTTP MCP server at `http://127.0.0.1:5080/mcp` while `run.ps1` is up. No token is required for this mock.
- **Cursor:** `.cursor/mcp.json` points at that local URL and contains no secrets. Reload MCP servers after the API is running.
- **ChatGPT connectors:** use the same MCP endpoint from a machine that can reach loopback. This repository does not publish the server.
- **Any other agent:** connect to the same URL and discover tools with `tools/list`.

Before real customer data, MCP must use the same server-side customer and role checks as REST. There are no sync, write, or Hangfire tools in this phase.
