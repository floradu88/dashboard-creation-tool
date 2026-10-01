# AI coding instructions

The root and folder-specific `AGENTS.md` files contain the maintained instructions. Tool adapters reference them so architectural rules have a single source. The mock application and scripts are implemented; live providers, persistence, and Hangfire are not.

## Tool setup

- **Claude Code:** Open this project. Root `CLAUDE.md` directs Claude to shared instructions; folder adapters identify local rules. Confirm applicable AGENTS.md files were read when changing a module. [Claude context documentation](https://support.claude.com/en/articles/14553240-give-claude-context-claude-md-and-better-prompts)
- **Rovo Dev:** Open the repository as the workspace. Rovo Dev uses `AGENTS.md` as project memory, including current/parent directory context. For cross-folder work, explicitly request root, ancestor and target-folder instructions; do not assume all descendants are loaded. No separate invented ROVO.md format is needed. This targets Rovo Dev coding tools, not automatic configuration of a hosted Rovo chat agent. [Rovo Dev memory](https://support.atlassian.com/rovo/docs/use-memory-in-rovo-dev-cli/)
- **Cursor:** Open the repository root. `.cursor/rules/project.mdc` is always applied; other `.mdc` rules use path globs and direct the agent to shared files. Check applicable rules in the installed client when starting implementation. [Cursor rules](https://cursor.com/docs/rules)

These files have been structurally checked; automatic loading has not been exercised inside the three clients. A useful first prompt is: “Read PLAN.md and root AGENTS.md, then all ancestor and local AGENTS.md files for the folders you will modify. Implement only the requested milestone and report checks performed.”

## Folder instruction map

Every folder below contains both `AGENTS.md` and a `CLAUDE.md` adapter, with a matching scoped Cursor rule.

| Folder | Responsibility |
| --- | --- |
| [src](../src/AGENTS.md) | Source projects |
| [src/CustomerDashboard.Api](../src/CustomerDashboard.Api/AGENTS.md) | API |
| [src/CustomerDashboard.Application](../src/CustomerDashboard.Application/AGENTS.md) | Application |
| [src/CustomerDashboard.Domain](../src/CustomerDashboard.Domain/AGENTS.md) | Domain |
| [src/CustomerDashboard.Infrastructure](../src/CustomerDashboard.Infrastructure/AGENTS.md) | Infrastructure |
| [src/CustomerDashboard.Infrastructure/MockData](../src/CustomerDashboard.Infrastructure/MockData/AGENTS.md) | JSON fixtures |
| [src/customer-dashboard-ui](../src/customer-dashboard-ui/AGENTS.md) | Angular UI |
| [src/customer-dashboard-ui/src/app/core](../src/customer-dashboard-ui/src/app/core/AGENTS.md) | Angular core |
| [src/customer-dashboard-ui/src/app/shared](../src/customer-dashboard-ui/src/app/shared/AGENTS.md) | Angular shared components |
| [src/customer-dashboard-ui/src/app/features](../src/customer-dashboard-ui/src/app/features/AGENTS.md) | Angular features |
| [src/customer-dashboard-ui/src/app/features/customers](../src/customer-dashboard-ui/src/app/features/customers/AGENTS.md) | Customer selection |
| [src/customer-dashboard-ui/src/app/features/dashboard](../src/customer-dashboard-ui/src/app/features/dashboard/AGENTS.md) | Dashboard |
| [src/customer-dashboard-ui/src/app/features/dashboard/news](../src/customer-dashboard-ui/src/app/features/dashboard/news/AGENTS.md) | Customer news |
| [src/customer-dashboard-ui/src/app/features/dashboard/market](../src/customer-dashboard-ui/src/app/features/dashboard/market/AGENTS.md) | Market intelligence |
| [src/customer-dashboard-ui/src/app/features/dashboard/account](../src/customer-dashboard-ui/src/app/features/dashboard/account/AGENTS.md) | Account management |
| [src/customer-dashboard-ui/src/app/features/dashboard/metrics](../src/customer-dashboard-ui/src/app/features/dashboard/metrics/AGENTS.md) | Customer metrics |
| [tests](../tests/AGENTS.md) | Tests |
| [tests/CustomerDashboard.UnitTests](../tests/CustomerDashboard.UnitTests/AGENTS.md) | Unit tests |
| [tests/CustomerDashboard.ApiTests](../tests/CustomerDashboard.ApiTests/AGENTS.md) | API tests |
| [tests/CustomerDashboard.IntegrationTests](../tests/CustomerDashboard.IntegrationTests/AGENTS.md) | Integration tests |
| [tests/e2e](../tests/e2e/AGENTS.md) | Browser tests |
| [scripts](../scripts/AGENTS.md) | PowerShell scripts |
| [docs](../docs/AGENTS.md) | Documentation |

## Maintenance

Edit shared AGENTS.md files when rules change; keep adapters as references. Use parent inheritance for ordinary new folders. When a new responsibility merits local rules, add its AGENTS.md, Claude adapter and scoped Cursor rule, then update this map. Shared instructions must remain consistent with PLAN.md and actual implementation status.

