# Repository Guidelines

## Anwendungsleitbild

GrimmDiff ist eine Git Software die Vollumfänglich git commands bereitstellt. Dazu kommt aber auch noch ein zusatz. Es analysiert und Gruppiert einen Diff nach Arbeitsschritten und gibt eine kurze 1 Satz beschreibung zu dem teil diff mit an.

## Repo Map

Do not read `README.md` as project or implementation context. It is a concise
GitHub landing page; use this file and the relevant source files instead.

### Find the relevant code and tests

- `src/GrimmDiff.Domain`: Git- und Diff-Domänenmodelle, Parser und Provider-Schnittstellen.
- `src/GrimmDiff.Infrastructure`: Git-CLI-Adapter und lokale, austauschbare Diff-Heuristik.
- `src/GrimmDiff.App`: WPF-Anwendung, MVVM-Viewmodel, lokale Einstellungen und UI.
- `tests/GrimmDiff.Tests`: ausführbare, paketfreie Unit-Tests für Parser und Heuristik.
- `scripts`: Auslieferungs- und lokale Wartungsskripte.


### UI-Styling

Use the in the projectfolde containing UI-DESIGN.md

### Repo Map Maintenance

Whenever a change adds, removes, renames, or repurposes a top-level directory or a major source/test area, update this Repo Map in the same change. Keep each entry aligned with the directory's actual contents and responsibilities; do not list generated or local-only directories as source areas unless they are relevant to repository structure.

