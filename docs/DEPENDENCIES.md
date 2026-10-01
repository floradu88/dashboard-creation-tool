# Dependency catalog and version policy

Status: phase-1 packages are installed and pinned in `Directory.Packages.props` and the npm lockfile. Rows below that are not in those files remain later-phase choices.

## Runtime baseline

- Backend: .NET 10 / ASP.NET Core 10, `net10.0`. Use SDK-default C# language version, nullable reference types and implicit usings.
- Frontend: Angular 22, standalone components, strict TypeScript; select a compatible exact patch of CLI, framework and Material/CDK together.
- Installed on this machine: .NET SDK 10.0.203, Node 24.15.0, npm 12.0.0 and PowerShell 7.6.5. SDKs 8 and 9 are also installed; `global.json` must select .NET 10.
- Angular 22's documented constraints include Node `^24.15.0`, TypeScript `>=6.0.0 <6.1.0`, and RxJS `^7.4.0` (among other supported branches). The installed Node meets that branch. Recheck before restore. [Angular compatibility](https://angular.dev/reference/versions)

## NuGet: mocked boilerplate

| Package | Project | Version family / purpose |
| --- | --- | --- |
| `Microsoft.AspNetCore.OpenApi` | Api | 10.0.12; first-party OpenAPI generation |
| `ModelContextProtocol.AspNetCore` | Api | 2.2.0; loopback streamable HTTP MCP server. [SDK](https://csharp.sdk.modelcontextprotocol.io/) |
| `ModelContextProtocol` | ApiTests | 2.2.0; MCP client used to list and call tools |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | Application, only if it owns registration extensions | 10.x; omit if registration stays in API |
| `Microsoft.Extensions.Options` | Infrastructure | 10.x; typed fixture/provider options |
| `Microsoft.Extensions.Logging.Abstractions` | Infrastructure | 10.x; provider diagnostics without choosing another logging stack |
| `Microsoft.AspNetCore.Mvc.Testing` | ApiTests | 10.0.x; in-process API host and HTTP tests |
| `Microsoft.NET.Test.Sdk` | UnitTests, ApiTests | Stable compatible release; standard `dotnet test`/VSTest integration |
| `xunit.v3` | UnitTests, ApiTests | Stable compatible release; test framework |
| `xunit.runner.visualstudio` | UnitTests, ApiTests | Compatible adapter for the chosen xUnit version; development-only assets |
| `coverlet.collector` | Test projects, optional | Add only if coverage reporting is configured and verified |

Use SDK/shared-framework capabilities for controllers, DI, configuration, JSON (`System.Text.Json`), HTTP, logging, health checks and Problem Details. Do not add redundant ASP.NET Core meta-packages or a JSON library just to reproduce built-in functionality. Keep Domain package-free initially. Infrastructure may require only the abstraction packages actually used; do not install every catalog entry automatically.

Use xUnit assertions and hand-written test doubles. No mediator, mapper, generic repository, mocking or assertion package is required. The test projects are xUnit v3 executables. `scripts/test.ps1` runs those assemblies directly because `dotnet test` reports that zero tests ran with this SDK and `global.json` test runner. [xUnit package guidance](https://xunit.net/docs/nuget-packages-v3)

OpenAPI metadata is sufficient initially; a documentation UI is optional. [ASP.NET OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0), [API test package](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing/)

## NuGet: later phases only

| Package | Trigger and placement |
| --- | --- |
| `Microsoft.EntityFrameworkCore` | Infrastructure when persistent snapshots arrive; 10.0.x |
| `Microsoft.EntityFrameworkCore.Design` | Migration tooling, development-only assets; same EF patch |
| `Microsoft.EntityFrameworkCore.SqlServer` OR `Npgsql.EntityFrameworkCore.PostgreSQL` | Select one after hosting/database decision; EF 10-compatible provider |
| `dotnet-ef` tool | Local tool manifest, version aligned with EF; no mandatory global install |
| `Microsoft.Extensions.Http.Resilience` | Infrastructure when real HTTP integrators arrive; compatible 10.x release |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | API when OIDC-issued bearer tokens protect real data; 10.0.x |
| `Hangfire.AspNetCore` | API/worker composition when scheduled ingestion arrives |
| `Hangfire.SqlServer` OR a vetted PostgreSQL Hangfire provider | Match chosen durable store; verify compatibility/licensing before selecting |
| `Microsoft.Data.SqlClient` OR `Npgsql` | Only if Alexis requires direct access using that database engine |
| OpenTelemetry hosting/exporter/instrumentation packages | Add only when hosted observability/backend is selected |

Use typed HTTP clients for Salesforce/Jira/Confluence/Slack/news/market APIs first. Adopt vendor SDKs only if a required workflow benefits from one. Provider package selection cannot resolve the unknown Alexis schema or news/market licensing. Avoid adding Hangfire/EF to Domain or Application. [Hangfire ASP.NET Core integration](https://docs.hangfire.io/en/latest/getting-started/aspnet-core-applications.html)

## npm dependencies

| Package/group | Purpose |
| --- | --- |
| `@angular/core`, `common`, `compiler`, `forms`, `platform-browser`, `router` | Framework packages, aligned Angular 22 patch |
| `@angular/material`, `@angular/cdk` | Components, accessibility and interaction primitives; compatible Angular 22 releases |
| `rxjs`, `tslib` | Compatible versions selected by Angular tooling |
| `echarts` | Graphs; integrate through one small shared Angular component and dispose chart instances |
| `@angular/cli`, `@angular/build`, `@angular/compiler-cli`, `typescript` | Local build/compiler tools in devDependencies |
| Angular CLI-supported unit runner and environment packages | Use generated compatible setup; verify components, state handling and HTTP tests |
| `@playwright/test` | Browser smoke tests; browser download is a separate setup step |
| `openapi-typescript` | Generate DTO types from OpenAPI; typed HttpClient services remain small and explicit |

Avoid an additional Angular chart wrapper unless needed; direct ECharts integration reduces peer-version coupling. Start without NgRx, SSR, a date library or a dashboard-builder framework. Use Intl for presentation, keeping business calculations on the server. Verify the chosen ECharts license and accessibility options before adding it. [ECharts documentation](https://echarts.apache.org/en/index.html)

## Reproducible builds

1. Resolve exact stable versions from official feeds during scaffolding; verify peer dependencies, runtime compatibility, license and vulnerability reports. Do not copy unverified search-result version numbers.
2. Put NuGet versions in root `Directory.Packages.props` with central package management; project references omit Version attributes. Keep framework-aligned Microsoft packages on a consistent servicing line.
3. Enable NuGet lockfiles and commit each `packages.lock.json`. Restore in locked mode in CI after lockfiles exist.
4. Pin exact direct npm versions, commit `package-lock.json`, and use `npm ci` after the initial lockfile-generating install. Specify Node/npm versions in project metadata/documentation.
5. Pin SDK in `global.json`, allowing an explicitly chosen patch roll-forward policy. Verify whether servicing the installed SDK is needed before a hosted release; do not silently replace machine-wide tools.
6. Document dependency updates and run appropriate builds/tests. Separate optional future packages from the phase-1 installation.

Registry connectivity and actual restore/build remain separate checks: having an SDK installed is not evidence that the complete project has built.
