# PowerShell scripts instructions

Scope: this directory and descendants. Read root and ancestor AGENTS.md files first.

Implement the setup, run, debug, test, stop, and validate scripts described in PLAN.md so they run on Windows PowerShell 5.1 and PowerShell 7. `install-prereqs.ps1` installs a missing .NET 10 SDK and Node.js 24 into the current user profile, without administrator rights, and announces each download. `setup.ps1` runs that install, then restores dependencies. The `.cmd` launchers start Windows PowerShell with a process-only execution-policy bypass and must not change the machine policy. Resolve paths from PSScriptRoot, handle spaces/ports, await readiness and preserve failure exit codes. Hide background windows and log under .local. Track owned child PIDs and validate ownership before stopping them. Debug instructions must include debugger attach, not only a Debug build. Do not alter global execution policy.

Follow PLAN.md for contracts and milestone boundaries. These folders currently establish structure/instructions; inspect actual implementation before claiming anything is runnable.

