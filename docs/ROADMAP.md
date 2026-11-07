# Development Plan

## 1. Problem recap

Windows resolves a file extension's icon and its "open" action through the same
registered ProgID. Many apps register themselves as the owner of generic formats
(`.htm`, `.xml`, `.csv`, …), so every extension that shares a ProgID ends up with
that app's icon even though several unrelated apps can open the same format.
Extension Icons' job is to let the icon be a property of the *file type*, decoupled
from whichever app currently claims the extension, and to let the user independently
choose which app handles which action (open/edit/text-edit/browse) per extension —
plus snapshot/restore ("fix mode") and shareable icon themes.

## 2. Stack and architecture decisions

| Decision | Choice | Why |
|---|---|---|
| Language | C# | Native Win32/registry/shell interop, first-class Visual Studio support, no cross-platform need (Win11-only by design) |
| Runtime | .NET 8 (LTS) | Current LTS, good perf, long support window |
| UI | WPF | Mature, simpler packaging than WinUI 3 (no MSIX requirement), abundant docs for the Win32/shell interop this app leans on. Revisit WinUI 3 for the Wishlist phase if a native Fluent look becomes a priority. |
| UI pattern | MVVM (CommunityToolkit.Mvvm) | Keeps registry/shell logic out of XAML code-behind and testable |
| Elevation | Per-user only for MVP (`HKCU\Software\Classes` + per-user `UserChoice` where writable) | Simpler, safer, no UAC friction; see open risk below for machine-wide (`HKLM`/`HKCR`) support in v1.0 |
| Packaging | Plain `.exe` via `dotnet publish` (self-contained) for MVP; installer (Inno Setup or MSI) by v1.0 | Fast iteration now, polish later |
| Localization | `.resx` resource files per culture (`en`, `es`), `CultureInfo`-driven at startup (OS language by default, overridable in-app) | Standard .NET/WPF localization path, no extra framework needed for two languages |

### Solution layout (already scaffolded)

```
ExtensionIcons.sln
src/
  ExtensionIcons.App/    WPF UI (views, viewmodels, App.xaml)
  ExtensionIcons.Core/   Domain logic: extension catalog, registry access,
                         icon extraction/injection, snapshot/restore, themes
tests/
  ExtensionIcons.Core.Tests/   xUnit tests against Core, via fake registry/shell providers
docs/
  ROADMAP.md             This file
```

`ExtensionIcons.Core` must never touch `Microsoft.Win32.Registry` or shell32
P/Invoke directly from logic that needs to be tested — wrap both behind small
interfaces (`IRegistryProvider`, `IShellNotifier`) with a real implementation and
an in-memory fake, so extension/ProgID resolution logic can be unit tested without
mutating a real machine's registry.

## 3. Biggest open technical risk — spike first

The exact registry mechanics for overriding **only the icon** of an extension,
without silently hijacking its "open" action, are version-dependent and partly
undocumented:

- Icon resolution and open-action resolution both flow through the same
  resolved ProgID for an extension (`HKCU\...\UserChoice` if present, else the
  extension's default value in `HKCR`). You generally can't point the icon at
  one ProgID and the open command at another using only a plain registry write.
- Since Windows 10 1703, `UserChoice` is hash-protected — Explorer validates a
  hash before honoring it, specifically to stop apps from silently rewriting
  default-app associations (anti browser-hijacking measure). This blocks the
  naive "just write the registry key" approach for *changing the default app*.
- The supported, user-consent-respecting way to change a default app is to
  invoke the OS's own UI (`ms-settings:defaultapps`, or
  `IApplicationAssociationRegistrationUI::LaunchAdvancedAssociationUI` via COM)
  rather than writing `UserChoice` directly.
- Icon-only overrides are more promising: create a dedicated per-extension
  ProgID (or override the existing one's `DefaultIcon` subkey) that forwards
  its `shell\open\command` to the real handler so functionality is unaffected,
  then force Explorer/the icon cache to refresh (`SHChangeNotify`, icon cache
  rebuild).

**First task of the MVP milestone is a time-boxed spike** to validate this
end-to-end on a real Windows 11 box (read current associations → override icon
only → confirm "open" behavior and the real owning app are both unaffected →
confirm the icon cache refreshes without a reboot) before any UI is built on
top of it. If direct `DefaultIcon` override turns out to be blocked the same
way `UserChoice` is, the fallback is a user-mode icon handler (`IconHandler`
COM shell extension) — more work, flagged here so it doesn't block planning.

**Spike results.** [Spike 001](spikes/001-icon-override-mechanism.md) and
[spike 002](spikes/002-icon-resolution.md) found that `UserChoice` governs the icon
as well as the open action, and that a per-extension dedicated ProgID only works
for extensions with no `UserChoice`. For extensions that have one (most of the
contested formats), step 5 overrides the `UserChoice` ProgID's own `DefaultIcon`
with a confirmation listing every extension that shares it; the per-extension
`IconHandler` stays the proper fix and needs its own spike before v1.0.

## 4. MVP (v0.1) — prove the core value, read-mostly, user-scope only

Goal: a user can see *why* an extension has the icon it has, and override the
icon per extension without touching default-app behavior, with a reliable undo.

- Enumerate registered file extensions with their current ProgID, owning app,
  and resolved icon (read-only inventory view).
- Bundle a small built-in catalog of common "contested" formats (`.htm`,
  `.html`, `.xml`, `.csv`, `.json`, `.txt`, …) with a sensible default icon per
  format, independent of any installed app.
- Let the user assign/override the icon for a given extension (user scope,
  `HKCU\Software\Classes`), with immediate Explorer refresh.
- Snapshot before every change automatically; one-click "fix mode" to revert
  an extension (or everything Extension Icons has touched) to its
  previously-captured state.
- No changes to default *open* app yet — out of scope until the spike in §3
  resolves how to do that safely.
- Core logic covered by unit tests against fake registry/shell providers.
- UI strings live in `.resx` resources from day one (English only for the
  MVP) so adding the Spanish translation in v1.0 is a translation task, not
  a refactor.

## 5. v1.0 — full association control, themes, installer

- Per-extension, per-action default app assignment (open / edit / text-edit /
  browse), using the OS-sanctioned flow (`ms-settings:defaultapps` deep link
  or `IApplicationAssociationRegistrationUI`) rather than raw `UserChoice`
  writes, so Windows' own consent/hash protections are respected.
- Optional machine-wide (`HKLM`/`HKCR`) icon overrides via an elevated helper
  process launched on demand (UAC prompt only for that operation), keeping the
  main app unprivileged.
- Icon themes: importable/exportable packs (zip of `.ico` files + a
  `theme.json` manifest mapping extension → icon), with validation (no
  arbitrary code execution, just image assets) before import.
- Expanded built-in extension catalog, contributed/curated.
- Full snapshot history (not just "last known good"), with named restore
  points the user can create manually (e.g., "before installing app X").
- Installer (MSI or Inno Setup), auto-update check.
- UI polish: search/filter extensions, bulk actions, icon preview at multiple
  sizes (16/32/48/256), basic light/dark theme support in the app itself.
- Diagnostics/log view for support requests (what changed, when, from what to
  what) — no telemetry sent anywhere, purely local.
- **Localization: English and Spanish supported in the UI**, selectable
  in-app and defaulting to the OS language; all v1.0 strings ship in both
  `.resx` sets, with Spanish reviewed by a native speaker before release.

## 6. Wishlist — beyond v1.0

- In-app browsing of a community icon-theme repository (GitHub-hosted),
  with ratings/previews before import.
- Background watcher (system tray) that detects when a newly installed app
  has silently re-claimed an extension's icon/association and offers a
  one-click revert using the existing snapshot mechanism.
- "Extension families" — manage related extensions (e.g. `.htm`/`.html`,
  `.jpg`/`.jpeg`) as one logical group by default, with per-member override
  when needed.
- PowerShell module / CLI for scripting association and icon changes
  (useful for IT rollouts, dotfiles-style repeatable setup).
- Enterprise story: GPO-deployable configuration profile for machine-wide
  rollout without per-user interaction.
- Plugin SDK for third parties to ship icon packs or extension-catalog
  extensions.
- Explorer context-menu quick actions ("Reset icon for this file type").
- WinUI 3 / Fluent UI rewrite if native Win11 look becomes a priority once
  the feature set has stabilized.
- **Content-based format recognition, beyond the extension** *(exploratory —
  not committed)*. Everything above identifies a file's type by its
  extension, which is really just a convention. A `.txt` can contain JSON, a
  `.dat` can be anything, and a renamed or extension-less file defeats the
  whole model. This would mean sniffing actual file content — magic-byte/
  signature detection (the same idea as the Unix `file` command / libmagic),
  plus structural checks for container formats where the signature alone
  isn't enough (e.g. `.docx`/`.xlsx`/`.jar`/`.apk` are all ZIP containers
  distinguished by what's inside them, not by their outer bytes) — to assign
  an icon/type based on what the file actually is, independent of its name.
  Needs its own research spike: where this plugs into Explorer (a Shell Icon
  Overlay/`IconHandler` COM object is the realistic hook, see below), and how
  to keep it fast enough not to make Explorer feel sluggish when listing a
  folder with thousands of files.
- **Per-file icons, including disambiguating one shared extension that
  several distinct formats claim** *(exploratory — not committed, depends on
  the item above)*. Today every override in this app is per-*extension*, not
  per-*file* — fine as long as one extension means one format. It breaks down
  for extensions reused across genuinely different formats (ambiguous `.dat`/
  `.bin` payloads, or schema-less `.xml`/`.json` where the "real" format is
  only knowable by sniffing content, e.g. an RSS feed vs. a config file vs. an
  SVG-as-XML). Solving this for real means moving from a static per-extension
  `DefaultIcon` registry entry to a live per-file decision, which is a much
  bigger architectural step: Explorer would need to call into this app (or a
  registered icon handler) *per file* rather than resolve a static registry
  value once per extension. That's the `IExtractIcon`/icon-handler COM
  contract, running inside `explorer.exe`'s process — real complexity and
  real performance risk (it would run once per visible icon, per folder
  view), so this is explicitly a "see how far content sniffing gets us
  first" item, not a planned feature.

## 7. Testing & quality approach

- `ExtensionIcons.Core` is pure logic + interfaces; no test touches the real
  registry. `ExtensionIcons.Core.Tests` uses in-memory fakes.
- Manual smoke-test checklist (real registry, real Explorer) run before each
  release, since icon-cache/shell-refresh behavior is hard to fully automate.
- Every destructive operation (anything that writes the registry) must go
  through the snapshot mechanism first — no exceptions — so "fix mode" is
  always trustworthy.

## 8. Rough sequencing

1. Spike: validate icon-only override mechanism (§3).
2. Core: registry/shell abstraction layer + fake providers + tests.
3. Core: extension inventory + built-in catalog.
4. App: read-only inventory UI.
5. Core + App: icon override (user scope) + snapshot/restore ("fix mode").
6. Ship MVP.
7. Core + App: default-app-per-action via OS-sanctioned flow.
8. Elevated helper for machine-wide changes.
9. Theme import/export.
10. Spanish translation (`.resx`) + in-app language switcher, reviewed by a
    native speaker.
11. Installer + polish.
12. Ship v1.0.
