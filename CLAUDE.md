# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**KidsAttendance** — Web system for tracking children's attendance in Sunday school classes. Built with ASP.NET Core MVC (.NET 10), SQL Server, and ASP.NET Identity.

## Build & Run Commands

```powershell
dotnet build KidsAttendance.slnx -v minimal   # Build all projects
dotnet run --project KidsAttendance.Web        # Run locally
dotnet restore KidsAttendance.slnx             # Restore NuGet dependencies
```

There are no automated tests yet. When added, use `dotnet test`.

## Database Setup

1. Run `database/001_Create_KidsAttendance_Database.sql` in SSMS to create `KidsAttendanceDb`.
2. Connection string is in `KidsAttendance.Web/appsettings.json` → `ConnectionStrings:KidsAttendanceDb`.
3. This is a **Database-First** project. To regenerate EF models after schema changes:

```powershell
dotnet ef dbcontext scaffold "Server=.;Database=KidsAttendanceDb;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer `
  --project KidsAttendance.Infrastructure `
  --startup-project KidsAttendance.Web `
  --context KidsAttendanceDbContext `
  --context-dir Persistence `
  --output-dir Persistence/Entities `
  --use-database-names `
  --data-annotations `
  --no-onconfiguring `
  --force
```

See `database/Scaffold-DbContext.md` for full notes.

## Architecture (Clean Architecture)

| Project | Responsibility |
|---|---|
| `KidsAttendance.Domain` | Core business model (minimal; mostly types) |
| `KidsAttendance.Application` | Use-case interfaces (`IClassGroupService`, `ISignatureService`) and DTOs |
| `KidsAttendance.Infrastructure` | EF Core (`KidsAttendanceDbContext`), Identity entities, service implementations, `AdminSeedService` |
| `KidsAttendance.Web` | ASP.NET Core MVC — controllers, views, ViewModels, static assets in `wwwroot/` |

Controllers inject `KidsAttendanceDbContext` directly (not repositories) alongside Application-layer services. This is intentional; do not introduce a repository abstraction unless explicitly requested.

## Key Domain Concepts

- **ClassGroup** — a Sunday school class group
- **Child** + **Guardian** — children and their guardians (many-to-many via `ChildGuardian`)
- **TeacherClassGroup** — assignment of a teacher (`AppUser`) to a `ClassGroup`
- **AttendanceSession** / **AttendanceRecord** — one session per class per date; one record per child per session
- **ChildGroupHistory** — tracks group changes over time

## Roles & Authorization

Roles are defined in `KidsAttendance.Infrastructure/Security/ApplicationRoles.cs`:
- `Coordinador` (Admin) — full access: groups, users, assignments, reports, Excel export
- `Teacher` — attendance check-in/check-out for their assigned group only
- `SnackTeam` — read-only view of present-count per group

Role constants are used with `[Authorize(Roles = ...)]` on controllers. Teachers are restricted to their active assigned group (`TeacherClassGroup`).

## Admin Seed

Configure in `appsettings.json` under `AdminSeed`. Set `Enabled: true` on first run, then set back to `false`. Uses `database/002_Seed_Admin_User.sql` alternatively.

## Coding Conventions

- 4-space indentation, UTF-8
- `PascalCase` for types/methods/properties, `camelCase` for locals/parameters, `_camelCase` for private readonly fields
- Controllers: `{Feature}Controller`; views under `Views/{Feature}/`
- Guard clauses for validation; keep methods focused

## Mandatory Tooling

- For C# changes: apply the `csharp-clean-code-skill`
- For new backend/frontend/JS/TS files: apply the `documentation-comments-skill`
- Use **Context7 MCP** at the start of any .NET/C# task to verify current .NET 10 / C# 14 APIs and patterns
- Use **sqlserver MCP** tools when inspecting or querying the database directly
