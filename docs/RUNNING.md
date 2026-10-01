# Running the mock dashboard

The local application is the JSON mock from PLAN.md milestones 1–3. It does not call Salesforce, Alexis, Jira, Confluence, Slack, news, or market providers, and it does not start a database or Hangfire.

## Prerequisites

Windows PowerShell 5.1 comes with Windows. No administrator account is required.

From the repository root:

```bat
scripts\install-prereqs.cmd
scripts\setup.cmd
scripts\run.cmd
```

`install-prereqs.cmd` installs only the missing tools. `setup.cmd` does that and then restores the .NET solution and runs `npm ci`. Both use a process-only execution-policy bypass so a default Restricted policy can run them. They do not change the machine execution policy.

The tools installed for the current user are:

- .NET SDK 10.0.203 from `global.json`, or a later 10.0 patch already installed, into `%USERPROFILE%\.dotnet`
- Node.js 24.15.0 into `%LOCALAPPDATA%\Programs\nodejs`. The official archive includes npm 11, so the script then installs npm 12.0.0 into that folder.

An existing Node.js 24 install at 24.15.0 or newer is kept. If it is writable and its npm is older than 12, npm 12.0.0 is installed into that same directory. The script adds user install folders to the user PATH and prints each download before it starts. `run.ps1`, `test.ps1`, and `validate-fixtures.ps1` also look in those folders, so the next command works in the same terminal.

PowerShell 7 can run the `.ps1` files directly when its execution policy already allows scripts:

```powershell
pwsh -NoProfile -File scripts/setup.ps1
pwsh -NoProfile -File scripts/run.ps1
```

| Surface | Address |
| --- | --- |
| UI | http://localhost:4200 |
| API | http://127.0.0.1:5080 |
| OpenAPI | http://127.0.0.1:5080/openapi/v1.json |
| Readiness | http://127.0.0.1:5080/health/ready |
| MCP | http://127.0.0.1:5080/mcp |

`run.cmd -Detach` leaves both processes running. Stop them with `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/stop.ps1`. `debug.ps1` is the same startup in the Debug configuration; attach the .NET debugger to the API process and use the Edge configuration in `.vscode/launch.json` for the UI. `-ApiPort` and `-UiPort` override the defaults and rewrite the UI proxy. Non-Mock mode is rejected at startup.

External chat agents connect to the MCP URL while the API is running. Client notes are in [MCP.md](MCP.md).

## Checks

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test.ps1 -E2E
```

`test.ps1` builds the solution, runs `CustomerDashboard.UnitTests` and `CustomerDashboard.ApiTests` through their executables, then runs the Angular unit tests and production build. `dotnet test` on this SDK reports that zero tests ran, so the script does not use it. `-E2E` also runs Playwright from `src/customer-dashboard-ui`. Install the browser once with `npx playwright install chromium` in that folder. The smoke spec covers customer switching, tab navigation, chart rendering, an invalid period, and a narrow viewport. That browser download timed out in the session that added the spec, so `-E2E` was not completed there. The same flows were exercised in a browser against the running UI.

## Fixtures

Normalized inputs live in `src/CustomerDashboard.Infrastructure/MockData`. The format for adding a customer is in [MockData/README.md](../src/CustomerDashboard.Infrastructure/MockData/README.md). Check a bundle before starting the API:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/validate-fixtures.ps1
```

The loader validates the same bundle again at startup. Files are copied to the API output. Slack is configured as unavailable in `appsettings.json`, so its records are omitted and the source stays marked unavailable. Customer news uses a 60-minute freshness window, so it is stale relative to a clock after `2026-09-29T09:00:00Z`.
