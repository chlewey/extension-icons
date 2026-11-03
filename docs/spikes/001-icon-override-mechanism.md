# Spike 001 — icon override mechanism

Roadmap step 1 (see [`ROADMAP.md §3`](../ROADMAP.md#3-biggest-open-technical-risk--spike-first)
and [§8](../ROADMAP.md#8-rough-sequencing)). Run live against a real Windows 11
machine, using a throwaway extension that Windows has never seen before, so
nothing real was touched. All test keys/files were created and removed by the
same script run.

## Goal

Confirm, empirically, the basic chain the whole app depends on:

1. `extension → ProgID → DefaultIcon` resolves the icon Explorer/shell
   reports.
2. Changing *only* `DefaultIcon` on that ProgID takes effect live (no
   reboot), after `SHChangeNotify(SHCNE_ASSOCCHANGED, ...)`.
3. Changing the icon does **not** disturb the ProgID's registered
   `shell\open\command`.

## Method

`HKCU:\Software\Classes` throwaway extension `.exticonspike123` → a
dedicated ProgID `ExtIcons.SpikeTest`, with its own `DefaultIcon` and
`shell\open\command` (pointed at `notepad.exe` as a stand-in "real
handler"). Resolved icon queried via `SHGetFileInfoW` with
`SHGFI_ICONLOCATION | SHGFI_USEFILEATTRIBUTES` (resolves by extension/type,
not by reading the actual file). Full script, including cleanup in a
`finally` block, is [`001-icon-spike.ps1`](001-icon-spike.ps1) alongside this
file.

## Results

| Step | Expected | Observed |
|---|---|---|
| Baseline, extension never registered | fallback/unknown icon | `iIcon = 0` (fallback) |
| After registering ProgID with `DefaultIcon = shell32.dll,41` | `iIcon = 41` | `iIcon = 41` ✅ |
| After changing *only* `DefaultIcon` to `shell32.dll,1`, refreshed via `SHChangeNotify` | `iIcon = 1`, no reboot needed | `iIcon = 1` ✅ |
| `shell\open\command` after the icon-only change | unchanged (`notepad.exe "%1"`) | unchanged ✅ |

One early run used an icon index (`,70`) that turned out not to resolve to a
real icon and silently fell back to a generic cached icon (index `309`)
instead of erroring — worth remembering for the real implementation: **icon
indices must be validated (e.g. by actually loading the icon) rather than
trusted blindly**, since an invalid index fails silently, not loudly.

Minor/non-blocking: `SHGetFileInfoW`'s `szDisplayName` (icon's containing
file path) came back empty in this script despite `iIcon` resolving
correctly — likely a P/Invoke struct-marshaling detail in the throwaway
PowerShell harness, not a shell behavior. Irrelevant to the conclusion since
`iIcon` alone was enough to prove resolution and refresh both work; worth
getting right when this logic moves into `ExtensionIcons.Core` (C# struct
marshaling, not PowerShell `Add-Type`, should avoid this class of issue).

## Conclusion

The basic chain works exactly as the roadmap assumed, for an extension with
**no existing `UserChoice`**: a dedicated ProgID's `DefaultIcon` can be
changed independently of its `shell\open\command`, and takes effect live via
`SHChangeNotify`. This validates the foundation `ExtensionIcons.Core`'s
registry layer (step 2) will be built on.

## Open questions — not yet answered, deliberately not tested against real extensions

This spike intentionally avoided touching any of the user's real,
already-associated extensions (`.htm`, `.xml`, …) — doing so would change
live behavior on their machine, even temporarily. Two real-world
complications remain open for step 2 (Core registry/shell abstraction
layer), to be investigated with throwaway ProgIDs/extensions again before
anything touches a real one:

1. **`UserChoice` precedence.** Once Windows has ever resolved a default for
   an extension, `HKCU\...\Explorer\FileExts\.ext\UserChoice` is set and is
   hash-protected (since Windows 10 1703) specifically to stop silent
   default-app hijacking. It's not yet confirmed whether `UserChoice`, once
   set, also governs *icon* resolution (not just the open command), which
   would mean a plain `HKCU\Software\Classes\.ext` override — or even a
   ProgID-level `DefaultIcon` edit — is ignored for any extension the user
   has already interacted with. (Community tools like SetUserFTA/SFTA
   demonstrate the hash itself is computable per Windows build, which is a
   candidate fallback if direct override turns out to be blocked — but that
   path deliberately works around an anti-hijacking protection and needs
   its own risk discussion before use.)
2. **Shared ProgIDs.** This is the README's actual complaint: `.htm` and
   `.html` (for example) can resolve to the *same* ProgID. Editing that
   ProgID's `DefaultIcon` directly would change the icon for every extension
   that shares it — the opposite of "distinct icon per extension." The
   likely correct mechanism is Explorer's per-*extension* icon-handler
   extensibility point (`HKCR\.ext\ShellEx\{BB2E617C-...}` `IconHandler`),
   which is documented as a per-extension hook independent of the ProgID's
   own `DefaultIcon` — but that requires a COM DLL implementing
   `IExtractIcon`, which is real implementation work, not a registry-only
   spike. Prototyping that is scoped into step 2, not this spike.
