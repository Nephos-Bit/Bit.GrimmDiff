# Repository Guidelines

## Anwendungsleitbild

GrimmDiff ist eine Git Software die Vollumfänglich git commands bereitstellt. Dazu kommt aber auch noch ein zusatz. Es analysiert und Gruppiert einen Diff nach Arbeitsschritten und gibt eine kurze 1 Satz beschreibung zu dem teil diff mit an.

## Repo Map

Do not read `README.md` as project or implementation context. It is a concise
GitHub landing page; use this file and the relevant source files instead.

### Find the relevant code and tests

- `src/GrimmDiff.Logic.Abstracts`: fachliche `Models`, Parser, Richtlinien und Ports ohne Infrastruktur- oder UI-Abhängigkeiten.
- `src/GrimmDiff.Logic`: Use-Case- und Analyse-Services auf Basis der Logic-Abstraktionen.
- `src/GrimmDiff.Data.Abstracts`: öffentliche Codex-Prozessmodelle und -Ports.
- `src/GrimmDiff.Data`: Git-, Codex- und Präferenzadapter.
- `src/GrimmDiff.Gui.Abstracts`: UI-Models und UI-Serviceverträge.
- `src/GrimmDiff.Gui`: WPF-Anwendung mit MVVM, DI-Start in `App.xaml.cs`, Controls, Renderern, Services, ViewModels und Views.
- `tests/GrimmDiff.Logic.Tests`, `tests/GrimmDiff.Data.Tests`, `tests/GrimmDiff.Gui.Tests`: ausführbare, paketfreie Tests der jeweiligen Schicht.
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

- `ReleaseNotes.md` trägt im Titel stets die Versionsnummer des nächsten Releases und enthält genau die Abschnitte `Deutsch` und `English` in dieser Reihenfolge.
- Bei jeder nutzerrelevanten Änderung ergänzt der Agent jeweils einen gleichwertigen deutschen und englischen Stichpunkt. Interne Refactorings und technische Commit-Namen gehören nicht in die Datei.
- Der Release-Workflow veröffentlicht die vorhandenen Notizen und legt danach automatisiert eine leere zweisprachige Datei für die nächste Patch-Version an. Der Agent ersetzt deshalb weder den Titel noch verwendet er einen Bereich `Unreleased`.
- Vor einem Release gleicht der Agent die Notizen mit dem Git-Diff seit dem letzten SemVer-Tag ab und hält pro Sprache drei bis sechs Stichpunkte ein.

