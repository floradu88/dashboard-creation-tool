# Ordered implementation backlog

Status: milestones 1–3 are implemented as a local JSON mock. Items 1–10 below are present in the repository. Live provider credentials and schemas remain out of scope. See [RUNNING.md](RUNNING.md) for the commands that were used to check the app.

| Order | Work item | Prerequisites | Completion evidence |
| --- | --- | --- | --- |
| 1 | Scaffold solution, four production projects and two test projects | .NET 10 SDK and NuGet access | Correct project references; successful build |
| 2 | Set central package versions, SDK pin, formatting, ignores and package locks | 1 | Clean repeatable restore/build |
| 3 | Scaffold Angular 22 workspace and Material/chart foundation | Compatible Node/npm and npm registry access | Production frontend build; intact instruction files |
| 4 | Define DTOs, data dictionary, canonical IDs and fixture schema | 1 | Reviewed metric definitions and valid typed fixtures |
| 5 | Implement one vertical slice: customers + overview from JSON through controller to UI | 3, 4 | Customer selector, KPI card, graph and extraction/source labels work end to end |
| 6 | Implement remaining source readers and query handlers | 5 | Filtered/paginated API results and meaningful calculation tests |
| 7 | Complete eleven tabs, source drill-down and state handling | 6 | All requested categories visible, accessible and responsive |
| 8 | Complete news/market/account/metric fixtures and edge cases | 7 | Provenance, periods, units, owners, targets, coverage and unavailable states verified |
| 9 | Implement setup/run/debug/test/stop PowerShell scripts and launch configurations | 5 onward | Startup, readiness, port override, breakpoints and cleanup verified on Windows |
| 10 | Contract/API/browser tests, documentation and CI | 7–9 | Clean-checkout workflow passes; README reflects real commands |

Begin with work item 1, then get the overview vertical slice working before broadening the UI. Keep source readers and DTOs compatible with later snapshot-backed implementations. Follow the dependency catalog; do not add EF/Hangfire/provider SDKs in the mocked phase.

## Environment readiness recorded 2026-09-29

| Check | Result |
| --- | --- |
| Working directory | `C:\code\projects\dashboard-creation-tool` |
| Existing application | Mock API, Angular UI, fixtures, tests, and scripts. Live providers are not implemented. |
| .NET SDK | 10.0.203 installed (also 8.0.420 and 9.0.314) |
| Node | 24.15.0 installed; within the documented Angular 22 Node range |
| npm | 12.0.0 installed |
| PowerShell | 7.6.5 installed |
| Git executable | Available; working folder is not yet a Git repository |
| Real integrations | Intentionally not needed for phase 1 |
| NuGet/npm feed connectivity | Public metadata reachable with network permission; restricted shell access was blocked |
| Angular registry metadata | npm reported core 22.2.0; exact package compatibility/restore still to verify |
| JSON preparation | Eight provider/catalog guides and matching minimal fictional fixtures supplied |
| Preparation validation | All eight JSON files parsed and matched their Markdown examples; customer IDs, sample worklog rate/allocation links, request links and local Markdown links checked |
| Instruction validation | 24 shared/Claude instruction pairs and 24 Cursor rule files structurally checked; client auto-loading not exercised |

Package restore and compilation are part of `scripts/setup.ps1` and `scripts/test.ps1`. Debugger attach is the VS Code compound in `.vscode/launch.json` after `scripts/debug.ps1`; that attach step is manual.

## Definition of ready

Architecture, directory responsibilities, dependency families, UI scope, DTO/provenance rules, provider JSON examples, local workflow and acceptance gates are documented. Local development runtimes are present and public registry metadata is accessible with network permission. Start creation using the backlog without waiting for the unresolved live-data business decisions in PLAN.md section 12.

## Definition of done for the mock boilerplate

The API and UI run from documented PowerShell commands, all eleven tabs use API-served fictional JSON, source timestamps remain truthful, core metrics and failure states are tested, customer changes cannot leak stale responses, the UI is responsive/accessibly navigable, and both API and Angular debugging work. Dependency lockfiles and meaningful validation results are committed when Git is initialized. Planning documents alone do not satisfy this definition.
