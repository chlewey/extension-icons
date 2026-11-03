# Contributing / branching conventions

## Branches

- `master` — always releasable. Hotfixes may be committed here directly when
  something is broken in a released version and needs an immediate,
  narrowly-scoped fix.
- `develop/xxxx` — roadmap-driven development towards a milestone (the MVP,
  v1.0, or a later version). This is where the work described in
  [`docs/ROADMAP.md`](docs/ROADMAP.md) happens. Example: `develop/mvp`,
  `develop/v1.0`.
- `feature/xxxx` — a new feature that is *not* part of the core roadmap
  (e.g. something from the wishlist, or an idea that comes up outside the
  planned milestones).
- `patch/xxxxx` — a fix or small patch that isn't urgent enough to go
  straight to `master` as a hotfix, but also isn't roadmap feature work.

Merge `develop/*` and `feature/*` branches back to `master` via pull request
once the milestone (or feature) is in a shippable state; `patch/*` branches
may be merged directly once reviewed.

## Progress tracking

See [`docs/PROGRESS.md`](docs/PROGRESS.md) for the current status of the MVP
and v1.0 milestones, tracked against the step list in
[`docs/ROADMAP.md`](docs/ROADMAP.md#8-rough-sequencing).
