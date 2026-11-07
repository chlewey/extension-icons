# Progress

Tracks the step list from [`ROADMAP.md §8`](ROADMAP.md#8-rough-sequencing).
One block per step. Update this file (and the checkbox) whenever a step is
completed — don't let it drift from reality.

## MVP (v0.1)

`[███░░░]` 3/6 — 50%

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
- [ ] 4. App: read-only inventory UI.
- [ ] 5. Core + App: icon override (user scope) + snapshot/restore
      ("fix mode").
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
