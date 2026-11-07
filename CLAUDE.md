# Extension Icons — project memory

Windows 11 app that decouples a file extension's icon from whichever app currently owns the association. Full plan: [docs/ROADMAP.md](docs/ROADMAP.md). User-facing docs: [README.md](README.md) (English) and [LEEME.md](LEEME.md) (Spanish).

## Stack
- C# / .NET 8 (`net8.0-windows`), WPF with MVVM (CommunityToolkit.Mvvm, referenced on `develop/mvp`; not yet on `master`)
- xUnit tests, coverlet for coverage
- Windows Registry + Shell APIs (`HKCU\Software\Classes`, per-user `UserChoice`)

## Layout
- `src/ExtensionIcons.App/` — WPF UI (views, viewmodels)
- `src/ExtensionIcons.Core/` — domain logic: catalog, registry access, icon handling, snapshot/restore, themes
- `tests/ExtensionIcons.Core.Tests/` — xUnit tests against Core using in-memory fakes
- `docs/ROADMAP.md` — architecture decisions, MVP / v1.0 / wishlist
- `site/` — static landing page for extension-icons.ilecto.app (Cloudflare Workers static assets; deploy from `site/` with `npx wrangler deploy`). Not part of the .NET solution.

## Commands
```powershell
dotnet build
dotnet test
```
Open `ExtensionIcons.sln` in Visual Studio 2022 to run/debug the app.

## Local dev environment (Windows 11)
- Required: .NET 8 SDK (`winget install Microsoft.DotNet.SDK.8`)
- Recommended: Visual Studio 2022 with ".NET Desktop Development" workload
- v1.0 installer: Inno Setup 6 (`winget install JRSoftware.InnoSetup`), installed per-user. Compiler: `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe` (not on PATH; call by full path). No `.iss` script exists yet. WiX is the MSI alternative if the roadmap changes.
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

## Branching (defined in CONTRIBUTING.md on `develop/mvp`)
- `master` — always releasable; narrow hotfixes only.
- `develop/xxxx` — roadmap work toward a milestone (e.g. `develop/mvp`, `develop/v1.0`).
- `feature/xxxx` — features outside the core roadmap (wishlist items, the landing page).
- `patch/xxxx` — non-urgent fixes.
- `develop/*` and `feature/*` merge to `master` by pull request when shippable.

## Current status
Roadmap work happens on `develop/mvp`; `master` lags it until the MVP is shippable. As of 2026-11-06, MVP steps 1-4 of 6 are done there (56 tests):
- Spike: icon-only override validated for an extension with no `UserChoice` (`docs/spikes/`). Open: whether `UserChoice` governs icons, and shared ProgIDs (`.htm`/`.html`).
- Core: registry/shell abstraction (`IRegistryProvider`, `IShellNotifier`, Win32 and Fake implementations), `ExtensionInventory`, `BuiltInCatalog` (11 formats), `IconLocation`/`OpenCommand` parsers, `InventoryFilter`.
- App: read-only inventory grid with search and two filters, UI strings in `Resources/Strings.resx`. Catalog icons are generated placeholders (`tools/generate-placeholder-icons.ps1`, PNGs in `src/ExtensionIcons.App/Assets/Icons/`).
- Known gap: packaged (AppX) ProgIDs show no icon or friendly name.

Next: step 5, icon override (user scope) plus snapshot/restore, the first step that writes to the registry. Plan a short spike on the two open questions first.
Live progress is tracked in `docs/PROGRESS.md` (on `develop/mvp`); check it rather than trusting this snapshot.

## Conventions
- Match surrounding code style; `Nullable` and `ImplicitUsings` are enabled in all projects.
- Keep README.md and LEEME.md in sync when either changes.
- Git: default branch is `master` (there is no `main`). Commit/push only when asked.
- Roadmap work goes on a `develop/*` branch, not directly on `master`.
- **Keep this file current on `master` at all times.** Whenever a milestone step lands, the stack, layout, environment or rules change, or the branch layout changes, update CLAUDE.md on `master` in the same working session, then commit and push it to `origin/master` (standing instruction from the owner, 2026-11-06). Update the "Current status" date too.
