# Spike 002 — UserChoice precedence and shared ProgIDs

Answers the two open questions left by [spike 001](001-icon-override-mechanism.md),
ahead of roadmap step 5 (icon override + snapshot/restore). Run on 2026-11-06 on a
real Windows 11 machine with the harness in
[`002-icon-resolution/`](002-icon-resolution/) (`dotnet run` from that folder).

**Safety.** Part A only reads. Part B writes only under `HKCU\Software\Classes` for
throwaway extensions (`.exticonspike002a/.b`) and throwaway ProgIDs
(`ExtIcons.Spike002.*`), and removes all of it in a `finally` block (verified: no
leftovers). No real extension or ProgID was modified.

Icons are read two ways, which agreed in every step once the shell had settled:
`AssocQueryString(ASSOCSTR_DEFAULTICON)` and `SHGetFileInfo(ICONLOCATION |
USEFILEATTRIBUTES)`.

## Question 1 — does `UserChoice` also govern the icon?

**Yes.** Read-only, against real extensions on this machine:

| Ext | `UserChoice` ProgID | class (default) ProgID | Icon the shell reports |
|---|---|---|---|
| `.htm` / `.html` | `MSEdgeHTM` | `htmlfile` (`iexplore.exe,-17`) | **Edge** (`msedge.exe,0`) |
| `.xml` | `MSEdgeHTM` | `xmlfile` | **Edge** (`msedge.exe,0`) |
| `.txt` | `AppX4ztfk9…` (Notepad) | `txtfilelegacy` (no icon) | Notepad's packaged icon |

When a `UserChoice` exists, the shell takes the ProgID from it and ignores the
extension's class default, for the icon exactly as for the open action. This is the
mechanism behind the README's complaint, and it is also why a class-default override
is useless for these extensions (see below). On this machine 145 of 948 extensions
have a `UserChoice`.

## Question 2 — can one extension's icon change without touching the others?

Two throwaway extensions A and B shared one ProgID whose `DefaultIcon` was
`shell32.dll,41`. Results for A, with B always staying at 41:

| # | Change | Icon of A | Verdict |
|---|---|---|---|
| 2 | `HKCU\Software\Classes\.A\DefaultIcon` | unchanged (41) | **ignored** |
| 3 | `HKCU\Software\Classes\SystemFileAssociations\.A\DefaultIcon` | unchanged (41) | **ignored** |
| 4 | A's class default points to its own ProgID with its own `DefaultIcon` | **changed (1)** | **works** |

In step 4 the dedicated ProgID's `shell\open\command` forwarded to the same program,
and `ASSOCSTR_EXECUTABLE` resolved to the same executable for A and B, so opening is
unaffected.

So the only registry-only per-extension mechanism that works is the **dedicated
ProgID as the extension's class default** (the roadmap's original idea). But it only
works while no `UserChoice` exists, because `UserChoice` names the ProgID and the
class default is then never consulted.

## Consequences for step 5

For the extensions this app exists for (`.htm`, `.html`, `.xml`, `.txt`, ...) a
`UserChoice` is typically present, so the dedicated-ProgID approach does not reach
them. The options, in order of preference:

1. **Dedicated ProgID** for extensions with no `UserChoice`. Safe, registry-only,
   per extension. Covers a minority of the contested formats.
2. **Override the `UserChoice` ProgID's own `DefaultIcon`** in
   `HKCU\Software\Classes\<ProgID>\DefaultIcon`. Works for the extension, but changes
   the icon of **every extension that resolves to that ProgID** (here `MSEdgeHTM` is
   shared by at least `.htm`, `.html` and `.xml`), which is the opposite of the goal.
   Acceptable only with an explicit confirmation listing the affected extensions. The
   inventory can compute that set. *Not tested live:* it relies on the documented rule
   that `HKCU\Software\Classes` overlays `HKLM\Software\Classes`; I did not edit a real
   ProgID to confirm.
3. **Per-extension `IconHandler` COM shell extension** (`ShellEx\{BB2E617C-…}`). The
   true per-extension hook, independent of the ProgID. Needs a native COM DLL
   implementing `IExtractIcon`; not prototyped. It is also the building block for the
   exploratory per-file icons wishlist item.
4. **Re-pointing `UserChoice`** to a dedicated forwarding ProgID. Only via the
   OS-sanctioned flow (step 7), never by writing the hash-protected key.

Recommended MVP scope: option 1 for extensions without `UserChoice`, option 2 with a
confirmation dialog for the rest, and a spike on option 3 before v1.0.

## Other findings worth carrying forward

- **Refresh is asynchronous.** After `SHChangeNotify(SHCNE_ASSOCCHANGED)`,
  `SHGetFileInfo` lagged `AssocQueryString`: the first, undelayed run still showed the
  old icon, and a 1.5 s wait after each notification made them agree (the exact
  settling time was not measured). The UI must not assume the icon has changed the instant the
  write returns.
- **The registry claim is not always what Explorer shows.** For `.csv`, the ProgID's
  `DefaultIcon` is `xlicons.exe,9` but `AssocQueryString` returned the generic
  `shell32.dll,0`. Not investigated (possibly the Office virtualized path).
  The inventory's "current icon" is the registry's claim; a true "what Explorer shows"
  needs the shell query used here.
- **Use `AssocQueryString` for `path,index`.** The spike-001 note about
  `szDisplayName` coming back empty was not just a PowerShell marshaling problem: with
  C# marshaling it is still empty in these runs. `iIcon` matched the resource index
  only for the `shell32.dll` throwaway icons; for Edge and Notepad it came back as 94
  and 86, which cannot be the index into `msedge.exe`/Notepad's icon (0), so it
  looks like an image-list index there (not confirmed).
- **Packaged apps** use `@{Package?ms-resource://...}` icon references, which
  `AssocQueryString` returns verbatim (as in `.txt` above).
