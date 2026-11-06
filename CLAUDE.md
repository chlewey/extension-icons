# Extension Icons — project memory

Windows 11 app that decouples a file extension's icon from whichever app currently owns the association. Full plan: [docs/ROADMAP.md](docs/ROADMAP.md). User-facing docs: [README.md](README.md) (English) and [LEEME.md](LEEME.md) (Spanish).

## Stack
- C# / .NET 8 (`net8.0-windows`), WPF with MVVM (CommunityToolkit.Mvvm planned, not yet referenced)
- xUnit tests, coverlet for coverage
- Windows Registry + Shell APIs (`HKCU\Software\Classes`, per-user `UserChoice`)

## Layout
- `src/ExtensionIcons.App/` — WPF UI (views, viewmodels)
- `src/ExtensionIcons.Core/` — domain logic: catalog, registry access, icon handling, snapshot/restore, themes
- `tests/ExtensionIcons.Core.Tests/` — xUnit tests against Core using in-memory fakes
- `docs/ROADMAP.md` — architecture decisions, MVP / v1.0 / wishlist

## Commands
```powershell
dotnet build
dotnet test
```
Open `ExtensionIcons.sln` in Visual Studio 2022 to run/debug the app.

## Local dev environment (Windows 11)
- Required: .NET 8 SDK (`winget install Microsoft.DotNet.SDK.8`)
- Recommended: Visual Studio 2022 with ".NET Desktop Development" workload
- Needed later (v1.0 installer): Inno Setup (`winget install JRSoftware.InnoSetup`) or WiX
- Optional: Windows SDK (`signtool`) for code signing
- Opening a new terminal after installing the SDK may be needed for `dotnet` to be on PATH.

## Rules (from the roadmap)
- `ExtensionIcons.Core` must not touch `Microsoft.Win32.Registry` or shell32 P/Invoke directly in testable logic. Go through `IRegistryProvider` / `IShellNotifier`, with a real implementation and an in-memory fake.
- No test may touch the real registry.
- Every operation that writes the registry must snapshot first, with no exceptions, so "fix mode" stays trustworthy.
- Do not write `UserChoice` directly (hash-protected since Windows 10 1703). Changing a default app goes through the OS-sanctioned flow (`ms-settings:defaultapps` or `IApplicationAssociationRegistrationUI`).
- MVP is per-user only (`HKCU`). Machine-wide changes (v1.0) use an elevated helper launched on demand; the main app stays unprivileged.
- UI strings live in `.resx` resources from day one. v1.0 must ship English and Spanish, selectable in-app and defaulting to the OS language; Spanish is reviewed by a native speaker.
- Themes are zip packs of `.ico` files plus a `theme.json`; validate on import (image assets only, no code execution).
- No telemetry. Diagnostics/logs stay local.

## Current status
Early scaffolding: solution, projects and one placeholder test exist; builds clean. Next milestone step: time-boxed spike to validate icon-only override (ROADMAP §3), then the registry/shell abstraction layer with fakes.

## Conventions
- Match surrounding code style; `Nullable` and `ImplicitUsings` are enabled in all projects.
- Keep README.md and LEEME.md in sync when either changes.
- Git: default branch is `master`. Commit/push only when asked.
