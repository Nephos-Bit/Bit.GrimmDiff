# GrimmDiff technical guide

## Requirements

- Windows 10 or later
- [Git for Windows](https://git-scm.com/download/win)
- .NET 11 SDK to build from source
- Optional: [Codex CLI](https://developers.openai.com/codex/cli/) signed in with ChatGPT for AI analysis

## Build from source

```powershell
git clone https://github.com/Nephos-Bit/Bit.GrimmDiff.git
cd Bit.GrimmDiff
dotnet run --project src/GrimmDiff.Gui
```

Run the test suite with:

```powershell
dotnet run --project tests/GrimmDiff.Logic.Tests
dotnet run --project tests/GrimmDiff.Data.Tests
dotnet run --project tests/GrimmDiff.Gui.Tests
```

To create a portable self-contained Windows archive:

```powershell
.\scripts\Publish-Portable.ps1
```

The archive is created under `artifacts/`.

## Create a release and updates

1. Choose a three-part SemVer version, for example `1.2.0`.
2. Create and push the matching tag: `git tag v1.2.0` and `git push origin v1.2.0`.
3. The GitHub Actions **Release** workflow builds the .NET 11 application, creates Velopack packages, and publishes the GitHub Release.
4. Download and test `GrimmDiff-Setup.exe` from the release. Future versions are then offered by the app on startup.

For a local package or a release recovery, install the matching Velopack CLI version and run:

```powershell
dotnet tool install -g vpk --version 1.2.161
$env:VPK_TOKEN = "<GitHub token with repository contents write access>"
.\scripts\Publish-Velopack.ps1 -Version 1.2.0 -VpkPath vpk
vpk upload github --repoUrl https://github.com/Nephos-Bit/Bit.GrimmDiff --outputDir .\artifacts\Releases --channel win --publish
```

The `Velopack` NuGet package and `vpk` must use the same version. Keep all generated update assets (`releases.win.json`, full and delta `.nupkg` files) attached to their GitHub Releases; renaming or deleting them breaks updates. Existing portable ZIP installs must be replaced once with `GrimmDiff-Setup.exe` before they can update themselves.

## Technology

- C# and .NET 11
- WPF
- Git CLI integration
- AvalonEdit and DiffPlex
- Codex CLI for optional AI analysis
