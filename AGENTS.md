# Repository Guidelines

## Anwendungsleitbild

GrimmDiff ist eine Git Software die Vollumfänglich git commands bereitstellt. Dazu kommt aber auch noch ein zusatz. Es analysiert und Gruppiert einen Diff nach Arbeitsschritten und gibt eine kurze 1 Satz beschreibung zu dem teil diff mit an.

## Repo Map

Do not read `README.md` as project or implementation context. It is a concise
GitHub landing page; use this file and the relevant source files instead.

### Find the relevant code and tests

- `src/GrimmDiff.Domain`: reine Git- und Diff-Domänenmodelle (`Models`), Richtlinien (`Policies`) sowie Parser (`Parsers`); keine UI-, Use-Case- oder Infrastrukturabhängigkeiten.
- `src/GrimmDiff.Application/Ports`: kleine, verbraucherorientierte Schnittstellen für Repository-, Analyse-, Codex- und Einstellungszugriffe.
- `src/GrimmDiff.Application/Models`: Application-spezifische Transport- und Statusmodelle.
- `src/GrimmDiff.Application/UseCases`: Repository-, Diff-Analyse-, Codex-Sitzungs- und Einstellungs-Use-Cases.
- `src/GrimmDiff.Infrastructure`: Implementierungen der Application-Ports; `Adapters/Git` und `Adapters/Codex` kapseln die CLIs, `Processors/DiffAnalysis` enthält die lokale Diff-Heuristik.
- `src/GrimmDiff.App`: WPF-Anwendung mit `Composition`, `Configuration`, `Commands`, `Controls`, `Models`, `Rendering`, `Services`, `ViewModels` und `Views`.
- `tests/GrimmDiff.Tests`: ausführbare, paketfreie Unit-Tests für Parser, Analyseprovider, Codex-Adapter und Application-Use-Cases.
- `scripts`: Auslieferungs- und lokale Wartungsskripte.
- `docs/compliance` und `docs/security`: versionierte CRA-Nachweise, Threat Model, sichere Nutzungs- und Incident-Response-Verfahren.
- `.github/workflows`: CI-Sicherheitsprüfungen für Tests, Dependency-Scan und SBOM-Erzeugung.


### UI-Styling

Use the in the projectfolde containing UI-DESIGN.md

### Repo Map Maintenance

Whenever a change adds, removes, renames, or repurposes a top-level directory or a major source/test area, update this Repo Map in the same change. Keep each entry aligned with the directory's actual contents and responsibilities; do not list generated or local-only directories as source areas unless they are relevant to repository structure.

### Quellcode-Organisation

- Jede C#-Datei enthält genau einen Top-Level-Typ. Mehrere Klassen, Records, Interfaces, Structs oder Enums in einer Datei sind unzulässig.
- Private, eng gekoppelte Hilfstypen dürfen ausschließlich als geschachtelte Typen innerhalb ihres Besitzertyps verbleiben.

### Release-Notizen

- `ReleaseNotes.md` enthält die sichtbaren Änderungen des nächsten Releases im Bereich `Unreleased`.
- Bei jeder nutzerrelevanten Änderung aktualisiert der Agent die deutschen und englischen Stichpunkte. Interne Refactorings und technische Commit-Namen gehören nicht in die Datei.
- Vor einem Release gleicht der Agent die Notizen mit dem Git-Diff seit dem letzten SemVer-Tag ab, ersetzt `Unreleased` durch die Release-Version und hält pro Sprache drei bis sechs Stichpunkte ein.

