# Progress

Tracks the step list from [`ROADMAP.md §8`](ROADMAP.md#8-rough-sequencing).
One block per step. Update this file (and the checkbox) whenever a step is
completed — don't let it drift from reality.

## MVP (v0.1)

`[█████░]` 5/6 — 83%

- [x] 1. Spike: validate icon-only override mechanism (§3). — see
      [`spikes/001-icon-override-mechanism.md`](spikes/001-icon-override-mechanism.md)
- [x] 2. Core: registry/shell abstraction layer + fake providers + tests. —
      `IRegistryProvider`/`IShellNotifier` with `Win32*` and `Fake*`
      implementations, 12 unit tests against the fakes; `Win32*` verified
      live against the real registry (not in the automated suite, per
      ROADMAP.md §7).
- [x] 3. Core: extension inventory + built-in catalog. — `ExtensionInventory`
      (extension → class/`UserChoice` ProgID, icon, description, open
      command) and `BuiltInCatalog` (11 contested formats), 12 new unit
      tests against the fake registry; inventory also run read-only against
      the live registry (948 extensions). Open items carried to step 4/5:
      catalog icon assets are not bundled yet (only icon keys), and icon
      locations can be packaged-app resource references
      (`@{...ms-resource://...}`, e.g. `.txt` → Notepad) as well as
      `file,index` paths.
- [x] 4. App: read-only inventory UI. — sortable `DataGrid` over all
      registered extensions (current icon, type, handling program, ProgID,
      where it was resolved from, catalog icon/format), with a search box
      and two filters (catalog formats only, default-app override only).
      MVVM via CommunityToolkit.Mvvm; strings in `Resources/Strings.resx`;
      filter/parsing logic lives in Core with 32 new unit tests (56 total).
      Catalog icons are generated placeholders
      (`tools/generate-placeholder-icons.ps1`); replace the PNGs in
      `src/ExtensionIcons.App/Assets/Icons/` with real artwork. Checked by
      launching the app against the live registry. Known gap: packaged
      (AppX) ProgIDs, e.g. Notepad's `.txt`, show no icon or friendly type
      name and the raw ProgID as the program; resolving those needs the
      package manifest.
- [x] 5. Core + App: icon override (user scope) + snapshot/restore
      ("fix mode"). — Built on [spike 002](spikes/002-icon-resolution.md).
      Every registry write goes through `SnapshotManager`, which saves a
      snapshot to `%LOCALAPPDATA%\ExtensionIcons\snapshots` *before* the first
      write, rolls back on failure, and can restore one snapshot, one
      extension, or everything ("Restore selected" / "Restore all"). Overrides
      (`IconOverrideService`, plan then apply): an extension without a
      `UserChoice` whose ProgID is simple enough to copy exactly gets its own
      ProgID (affects only that extension); anything else (a `UserChoice`, or
      a ProgID with DDE, `DelegateExecute`, shell extensions or non-text
      values) changes only the existing ProgID's `DefaultIcon`, which never
      affects opening, and asks for confirmation listing every extension that
      shares it. Chosen icons are copied to `%LOCALAPPDATA%\ExtensionIcons\icons`
      so they outlive the original file. UI: "Use catalog icon", "Choose icon
      file...", "Restore selected", "Restore all (fix mode)". 116 unit tests
      (fake registry); also run against the real registry on throwaway
      extensions (override, DDE fallback, revert byte-for-byte, restore from a
      fresh process) and through the real window with UI Automation. Not
      exercised live: changing a ProgID that is defined machine-wide (HKLM),
      which relies on the documented HKCU-over-HKLM merge, and the shared-icon
      confirmation dialog (covered by unit tests only). Try it first on an
      unimportant extension; "Restore all" undoes it. Refresh in Explorer is
      asynchronous (`SHChangeNotify`).
- [ ] 6. Ship MVP.

## v1.0

`[░░░░░░]` 0/6 — 0%

- [ ] 7. Core + App: default-app-per-action via OS-sanctioned flow.
- [ ] 8. Elevated helper for machine-wide changes.
- [ ] 9. Theme import/export.
- [ ] 10. Spanish translation (`.resx`) + in-app language switcher, reviewed
       by a native speaker.
- [ ] 11. Installer + polish.
- [ ] 12. Ship v1.0.
