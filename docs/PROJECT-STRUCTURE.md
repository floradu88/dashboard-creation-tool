# Project creation blueprint

The source/concern directories, agent instructions and eight minimal JSON inputs exist. The solution, project files, Angular workspace, scripts and complete demonstration dataset will be created in the implementation sequence. Preserve existing instructions and fixtures while scaffolding into these non-empty directories; never force-overwrite a whole folder. See [JSON input contracts](json-input/README.md) for field definitions and examples.

## .NET projects and references

| Project | Type | Project references |
| --- | --- | --- |
| CustomerDashboard.Domain | net10.0 class library | None |
| CustomerDashboard.Application | net10.0 class library | Domain |
| CustomerDashboard.Infrastructure | net10.0 class library | Application, Domain |
| CustomerDashboard.Api | net10.0 web API with controllers | Application, Infrastructure |
| CustomerDashboard.UnitTests | net10.0 test project | Domain, Application |
| CustomerDashboard.ApiTests | net10.0 test project | Api |
| CustomerDashboard.IntegrationTests | Later test project | Infrastructure and necessary host |

Create `CustomerDashboard.slnx`; add only implemented projects. Organize handlers and DTOs by feature under Application (Customers, Overview, Commercial, Usage, Slas, Delivery, Requests, News, Market, Accounts, Metrics, Sources). Keep shared types limited to actual shared concepts.

Infrastructure starts with `Configuration/`, `MockData/` and `DataServices/`. Later add `Integrations/{Provider}/`, `Persistence/`, `Mappings/` and `Scheduling/` when those capabilities exist. Add narrow folder instructions then; empty speculative projects are unnecessary.

API starts with `Controllers/`, `Configuration/`, `Program.cs`, `appsettings.json`, `appsettings.Development.json` and `Properties/launchSettings.json`. Register Problem Details, exception handling, controllers, fixture services, OpenAPI and readiness checks. Configuration errors must surface clearly before serving invalid dashboard data.

## Angular workspace

Location: `src/customer-dashboard-ui`. Create the workspace in place without deleting its existing guidance. Use SCSS, routing, strict mode and standalone components. Include `angular.json`, TypeScript configs, package files and a development proxy. Follow a feature-oriented app structure:

```text
src/app/
  core/                 API transport, route/config services; later auth
  shared/               cards, charts, provenance, state and table components
  features/
    customers/          customer selector/search
    dashboard/
      overview/
      commercial/
      usage/
      slas/
      delivery/
      requests/
      news/
      market/
      account/
      metrics/
      sources/
```

Ordinary tab folders inherit dashboard instructions. News, market, account and metrics have extra rules for their distinct data semantics. Share shell/filter state but avoid a single component handling all tabs. Keep backend DTO generation in a clearly marked generated folder; changes originate in OpenAPI, not manual edits to generated files.

## Root build/config files to create

| File | Contract |
| --- | --- |
| `global.json` | Select the verified .NET 10 SDK and patch policy |
| `Directory.Build.props` | Nullable, implicit usings, deterministic build, lockfile policy; CI warning policy after baseline validation |
| `Directory.Packages.props` | Exact centrally managed NuGet versions |
| `.editorconfig` | C#/TS/JSON/Markdown formatting; UTF-8 and consistent line endings |
| `.gitignore` | bin/obj, node_modules, dist, .angular, .local, test output, local secrets and local agent memory |
| `.config/dotnet-tools.json` | Add only when local tools such as dotnet-ef are used |
| `.vscode/launch.json`, `tasks.json` | API attach/browser debug and documented process ownership |

No Git metadata was found during inspection. Files can be created normally; repository initialization/remote selection is separate from the application architecture. Do not invent a remote or publish changes.

## Application configuration contract

- `DataSources:{Provider}:Mode`: Mock/Live independently; validate supported modes and required fields.
- `DataSources:{Provider}:FreshnessMinutes`: agreed source-specific thresholds.
- `MockData:RootPath`: default content-root-relative fixture location.
- `MockData:Scenario`: development-only deterministic scenario selection.
- `Reporting:DefaultCurrency`, `Reporting:TimeZone`: explicit defaults, with validated customer overrides.
- `IntegrationScheduling:Enabled`: false until Hangfire is implemented.
- API/UI ports default to 5080/4200 and can be overridden by scripts.
- Later: authentication authority/audience, permitted origins, application database connection, source credentials/scopes and worker settings. Keep secret values out of committed settings.

## Validation and operational deliverables

Phase 1 must have a clean restore/build/test path, readiness endpoint, OpenAPI output, working Angular proxy, published fixture files and owned-process cleanup. Document both PowerShell startup and actual debugger attach. CI should restore locked packages, build backend/frontend, run unit/API tests, then a focused browser smoke suite with deterministic fixture configuration. Choose the CI platform when the repository host is established.
