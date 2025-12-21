# AGENTS.md

Instructions for AI agents (Claude Code, OpenCode, etc.) working with this repository.

## Project Overview

**SystemTray** is a cross-platform system tray icon library for .NET 10.

### Current Status
- ✅ **Linux implementation** (`Olbrasoft.Linux.SystemTray`) - D-Bus StatusNotifierItem protocol
- 🚧 **Windows implementation** - Planned (`Olbrasoft.Windows.SystemTray`)
- 🚧 **macOS implementation** - Planned (`Olbrasoft.MacOS.SystemTray`)

### Architecture
- `src/Olbrasoft.Linux.SystemTray/` - Linux D-Bus implementation (AppIndicator3)
- `tests/Olbrasoft.Linux.SystemTray.Tests/` - xUnit tests
- Clean C# interface hides D-Bus complexity

## Build Commands

```bash
# Build all projects
dotnet build

# Run tests
dotnet test

# Pack NuGet packages
dotnet pack -c Release -o ./nupkg

# Publish to NuGet (requires API key)
dotnet nuget push ./nupkg/*.nupkg --api-key <KEY> --source https://api.nuget.org/v3/index.json
```

## Code Style & Standards

**CRITICAL: Follow Engineering Handbook rules!**

📚 **Reference:** `/home/jirka/GitHub/Olbrasoft/engineering-handbook/development-guidelines/`

### Key Rules

1. **Project Structure**
   - Source: `src/{ProjectName}/`
   - Tests: `tests/{ProjectName}.Tests/`
   - **Each source project = separate test project** (NEVER shared test project)

2. **Naming Convention**
   - **Folders:** `SystemTray.Linux/` (NO `Olbrasoft.` prefix)
   - **Namespaces:** `Olbrasoft.SystemTray.Linux` (WITH `Olbrasoft.` prefix)
   - Set via: `<RootNamespace>Olbrasoft.{Domain}.{Layer}</RootNamespace>`

3. **Testing**
   - Framework: **xUnit + Moq** (NOT NUnit/NSubstitute)
   - Pattern: `{ClassName}Tests.cs` (e.g., `TrayIconTests.cs`)
   - Mirror source folder structure in tests

4. **.NET Version**
   - Target: `net10.0`
   - Nullable: `enable`
   - ImplicitUsings: `enable`

5. **SOLID Principles**
   - Follow patterns from `solid-principles-2025.md`
   - Use dependency injection
   - Interface-based design

## Important Paths

| Path | Description |
|------|-------------|
| `src/` | Source projects |
| `tests/` | Test projects |
| `Olbrasoft.Linux.SystemTray.sln` | Solution file |
| `README.md` | User documentation |
| `IMPLEMENTATION.md` | Technical implementation details |

## Engineering Handbook

**Before making changes, review:**

- [`dotnet-project-structure.md`](https://github.com/Olbrasoft/engineering-handbook/blob/main/development-guidelines/dotnet-project-structure.md) - Project layout
- [`workflow-guide.md`](https://github.com/Olbrasoft/engineering-handbook/blob/main/development-guidelines/workflow-guide.md) - Git workflow, branches, commits
- [`github-repository-setup.md`](https://github.com/Olbrasoft/engineering-handbook/blob/main/development-guidelines/github-repository-setup.md) - Repository configuration

## Current Implementation Status

### Linux (StatusNotifierItem)
- ✅ D-Bus integration via `Tmds.DBus.Protocol`
- ✅ Icon rendering with SkiaSharp
- ✅ Interface: `ITrayIcon`, `ITrayMenu`, `IIconRenderer`
- ✅ Multi-icon support via `ITrayIconManager`
- ✅ SVG icon support
- ✅ Animation support

### TODO
- [ ] Windows implementation (`System.Windows.Forms.NotifyIcon`)
- [ ] macOS implementation (`NSStatusItem`)
- [ ] Core abstraction layer (`SystemTray.Core`)
- [ ] Meta-package (`Olbrasoft.SystemTray`)

## Secrets

**NEVER commit secrets to Git!**

Use:
- `dotnet user-secrets` for local development
- GitHub Secrets for CI/CD
- Environment variables for production

## Deployment

**Tests must pass before deployment:**

```bash
dotnet test
# If all pass:
./deploy/publish-nuget.sh  # (when created)
```

## Issues & Sub-Issues

- Use native GitHub sub-issues (NOT markdown checkboxes)
- Each task step = separate sub-issue
- Close sub-issues immediately when done
- Close parent issue only after: tests pass + deployed + user approval

## Questions?

Check Engineering Handbook first:
```bash
cd ~/GitHub/Olbrasoft/engineering-handbook
cat development-guidelines/dotnet-project-structure.md
```
