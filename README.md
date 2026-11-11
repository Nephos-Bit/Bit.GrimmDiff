# GrimmDiff

**Turn a noisy Git diff into a clear account of the work behind it.**

GrimmDiff is a native Windows desktop app for exploring Git changes by intent. It reads the current working tree or a historical commit, groups related hunks into coherent work steps, and gives every group a concise explanation. The result is a faster path from “what changed?” to “why does this change belong together?”

![Platform](https://img.shields.io/badge/platform-Windows-0078D4?logo=windows&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![License](https://img.shields.io/badge/license-MIT-green)

## Why GrimmDiff?

Traditional diffs are organized by file. Real implementation work often is not. A feature, refactor, or bug fix can span contracts, implementations, tests, and configuration. GrimmDiff brings those related changes back together so reviews, handovers, and self-review start with the work step—not a flat file list.

## Highlights

- **Work-step grouping** — groups related diff hunks by implementation intent instead of only by file path.
- **Two analysis modes** — start with a local rule-based preview; use GPT-6 Luna through the Codex CLI when a deeper grouping is useful.
- **Current and historical context** — inspect staged, working-tree, untracked, or selected historical changes.
- **Focused diff reader** — browse grouped files and rendered diffs without losing the surrounding repository context.
- **Local-first fallback** — if Codex is unavailable or a diff is too large, GrimmDiff continues with local grouping.
- **Sensitive-file protection** — diffs for `.env` files and common private-key, certificate, and password-vault formats are analyzed locally.
- **A quiet native UI** — a fast WPF interface with dark, light, and system themes plus German and English analysis output.

## How it works

1. Open or clone a local Git repository.
2. Select the current changes or a commit from the recent history.
3. GrimmDiff builds a local preview and groups changed sections into work steps.
4. Optionally run AI analysis for a refined grouping and one-sentence description per step.
5. Select a file within a group to inspect its diff.

AI analysis is opt-in per repository. Before sending anything, GrimmDiff asks for explicit approval. It sends only the relevant diff data to the signed-in ChatGPT workspace; it does not grant unrestricted access to other repository files.

## Requirements

- Windows 10 or later
- [Git for Windows](https://git-scm.com/download/win)
- Optional: the [Codex CLI](https://developers.openai.com/codex/cli/) signed in with ChatGPT, for AI-powered grouping

## Install

Download the latest Windows build from [Releases](https://github.com/Nephos-Bit/Bit.GrimmDiff/releases), extract it, and run `GrimmDiff.App.exe`.

No Codex CLI is required to use the app: local, rule-based grouping remains available.

## Build from source

Install the .NET 10 SDK, then run:

```powershell
git clone https://github.com/Nephos-Bit/Bit.GrimmDiff.git
cd Bit.GrimmDiff
dotnet run --project src/GrimmDiff.App
```

Run the test suite with:

```powershell
dotnet run --project tests/GrimmDiff.Tests
```

To create a self-contained Windows package:

```powershell
.\scripts\Publish-Portable.ps1
```

The archive is created under `artifacts/`.

## Technology

- C# and .NET 10
- WPF
- Git CLI integration
- AvalonEdit and DiffPlex
- Codex CLI for optional GPT-6 Luna analysis

## Contributing

Issues and pull requests are welcome. For bugs or ideas, open an [issue](https://github.com/Nephos-Bit/Bit.GrimmDiff/issues) with the repository state, expected behavior, and a minimal reproduction where possible.

## License

Released under the [MIT License](LICENSE).
