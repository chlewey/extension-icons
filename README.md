# Extension Icons

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2011-0078D6)](#)

A Windows 11 application that gives every file extension a consistent, correct
icon — independent of whichever app currently claims it.

*Lea esto en español: [LEEME.md](LEEME.md)*

## The problem

Windows resolves a file extension's icon through whichever app last registered
itself as that format's handler. In practice this means many file types end up
borrowing an icon that has nothing to do with the format itself:

- `.htm` and `.html` show up as "Edge documents", even though an HTML file is
  not an Edge-specific format.
- `.xml`, `.csv`, `.json` and similar general-purpose formats are routinely
  rebranded with the icon of whatever app last installed itself as their
  handler.
- Installing a new "aggressive" application can silently take over icons and
  associations for extensions it has no special claim to.

Extension Icons treats the icon as a property of the **file type**, not of the
app that happens to be registered for it right now — and lets you decide,
per extension, which installed application should handle which action.

## What it does

- **Distinct icons per file type.** Keeps a consistent icon for common
  extensions regardless of which application currently owns the association.
- **Capability-aware.** Recognizes which installed applications are able to
  handle a given extension, not just which one is currently the default.
- **Per-action defaults.** Lets you choose, per file type, which application
  should be used to open, edit, text-edit, or browse a file.
- **Fix mode.** Reverts icons and associations to a previously saved state —
  useful right after installing an application that aggressively claims file
  types it shouldn't.
- **Themes.** Supports sets of file-type icons, including themes downloaded
  from community repositories.

## Status

Early development. See [`docs/ROADMAP.md`](docs/ROADMAP.md) for the full
development plan, architecture decisions, and the MVP / v1.0 / wishlist
breakdown.

## Tech stack

- C# / .NET 8
- WPF (MVVM)
- Windows Registry & Shell APIs (`HKEY_CLASSES_ROOT`, per-user `UserChoice`)

## Getting started

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and
Visual Studio 2022 (or later) with the ".NET Desktop Development" workload.

```powershell
git clone https://github.com/chlewey/extension-icons.git
cd extension-icons
dotnet build
```

Open `ExtensionIcons.sln` in Visual Studio to run or debug the app.

## Localization

Starting with v1.0, the interface will support **English** and **Spanish**,
selectable in-app and defaulting to the operating system's language.

## License

[MIT](LICENSE)
