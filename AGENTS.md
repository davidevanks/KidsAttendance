# Repository Guidelines

## Project Structure & Module Organization
This repository contains a .NET 10 solution using Clean Architecture:

- `KidsAttendance.Web/`: ASP.NET Core MVC UI (controllers, views, viewmodels, static assets in `wwwroot/`).
- `KidsAttendance.Application/`: application contracts and use-case interfaces.
- `KidsAttendance.Domain/`: domain layer (core business model; currently minimal).
- `KidsAttendance.Infrastructure/`: EF Core persistence, Identity entities, and service implementations.
- `database/`: SQL scripts (`001_Create_KidsAttendance_Database.sql`, admin seed, scaffold notes).

Keep feature work grouped by layer, not by file type across the whole repo.

## Build, Test, and Development Commands
- `dotnet build KidsAttendance.slnx -v minimal`  
  Builds all projects and validates references.
- `dotnet run --project KidsAttendance.Web`  
  Runs the MVC app locally.
- `dotnet restore KidsAttendance.slnx`  
  Restores NuGet dependencies.
- `dotnet ef dbcontext scaffold ...`  
  Regenerates Database-First models (see `database/Scaffold-DbContext.md`).

## Coding Style & Naming Conventions
- Use 4-space indentation and UTF-8 text files.
- C# naming: `PascalCase` for types/methods/properties, `camelCase` for locals/parameters, `_camelCase` for private readonly fields.
- Controllers: `{Feature}Controller`; views under matching folder (`Views/{Feature}/`).
- Keep methods focused and explicit; prefer guard clauses for validation.

## Testing Guidelines
There is currently no test project in the solution. For new tests:
- Prefer xUnit with a separate `KidsAttendance.Tests` project.
- Name test files `{ClassName}Tests.cs` and methods `MethodName_ShouldExpectedBehavior`.
- Run with `dotnet test` once tests exist.

## Commit & Pull Request Guidelines
Git history is not yet standardized; use this convention moving forward:
- Commit format: `type(scope): summary` (e.g., `feat(attendance): add checkout signature capture`).
- Keep commits small and task-focused.
- PRs should include: purpose, affected layers, DB/script changes, manual test steps, and screenshots for UI changes.

## Security & Configuration Tips
- Do not commit real credentials; keep production secrets out of `appsettings*.json`.
- Validate role/group access in backend controllers and services (not only UI).
- For admin seeding, use `database/002_Seed_Admin_User.sql` and rotate credentials after first login.

## Mandatory Tooling
- Use relevant repository skills by default. For C# changes, use `$csharp-clean-code-skill`; for new backend/frontend/JS/TS files, use `$documentation-comments-skill`.
- Use Context7 MCP at the start of any C#/.NET task to verify current .NET 10/C# 14 APIs, syntax, async/DI/logging/controller patterns.
- If Context7 guidance conflicts with this repository, prefer existing repository conventions unless the change is explicitly requested and compatible.
- Use MCP/tools when they provide authoritative local or connected context; do not guess when a tool can verify.