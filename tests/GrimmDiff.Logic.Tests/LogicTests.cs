using System.IO;
using System.Text.Json;
using GrimmDiff.Data.Abstracts;
using GrimmDiff.Gui;
using GrimmDiff.Logic;
using GrimmDiff.Logic.Abstracts;
using GrimmDiff.Data;

var failures = new List<string>();

void Test(string name, Action action)
{
    try
    {
        action();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

Test("release notes parse bilingual sections", () =>
{
    const string notes = """
                         # GrimmDiff 1.2.3

                         ## Deutsch

                         - Neue Ansicht für Änderungen.
                         - Verbesserte Update-Hinweise.
                         - Wichtige Fehlerbehebungen.

                         ## English

                         - New view for changes.
                         - Improved update notices.
                         - Important bug fixes.
                         """;
    var parsed = ReleaseNotesReaderService.Parse(notes) ?? throw new Exception("Expected valid release notes.");
    if (parsed.Version != "1.2.3" || !parsed.ForLanguage("Deutsch").Contains("Neue Ansicht") ||
        !parsed.ForLanguage("English").Contains("New view")) throw new Exception("Expected localized notes.");
});
Test("release notes reject malformed content", () =>
{
    const string notes = """
                         # GrimmDiff 1.2.3

                         ## Deutsch

                         - Nur ein Punkt.

                         ## English

                         - Only one item.
                         """;
    if (ReleaseNotesReaderService.Parse(notes) is not null) throw new Exception("Expected malformed notes to be ignored.");
});

const string diff =
    "diff --git a/src/Foo.cs b/src/Foo.cs\nindex 123..456 100644\n--- a/src/Foo.cs\n+++ b/src/Foo.cs\n@@ -4,2 +4,3 @@ class Foo\n-  void Run() {}\n+  // SharedProcessor process data\n+  void ProcessData() {}\ndiff --git a/src/Bar.cs b/src/Bar.cs\nindex 123..456 100644\n--- a/src/Bar.cs\n+++ b/src/Bar.cs\n@@ -1 +1 @@\n-// old\n+// SharedProcessor process data\n";
Test("parser returns hunks", () =>
{
    var hunks = UnifiedDiffParser.Parse(diff, DiffScopeModel.WorkingTree);
    if (hunks.Count != 2 || hunks[0].FilePath != "src/Foo.cs") throw new Exception("Unexpected hunk parse result.");
});
Test("parser retains file changes without text hunks", () =>
{
    const string source =
        "diff --git a/assets/logo.png b/assets/logo.png\nnew file mode 100644\nindex 0000000..1234567\nBinary files /dev/null and b/assets/logo.png differ\n";
    var hunks = UnifiedDiffParser.Parse(source, DiffScopeModel.WorkingTree);
    if (hunks.Count != 1 || hunks[0].Header != "File metadata" || !hunks[0].Content.Contains("Binary files"))
        throw new Exception("Expected binary file change.");
});
Test("heuristic groups related text", () =>
{
    var result = new LocalHeuristicDiffAnalysisService()
        .AnalyzeAsync(new DiffAnalysisRequestModel(".", DiffScopeModel.WorkingTree, diff, "en")).GetAwaiter().GetResult();
    if (result.Groups.Count != 1) throw new Exception("Expected semantic group.");
});
Test("unrelated hunks fall back to separate groups", () =>
{
    const string source =
        "diff --git a/a.cs b/a.cs\n@@ -1 +1 @@\n+// network transport\ndiff --git a/b.cs b/b.cs\n@@ -1 +1 @@\n+// invoice ledger\n";
    var result = new LocalHeuristicDiffAnalysisService()
        .AnalyzeAsync(new DiffAnalysisRequestModel(".", DiffScopeModel.WorkingTree, source, "de")).GetAwaiter().GetResult();
    if (result.Groups.Count < 2) throw new Exception("Expected fallback grouping.");
});
Test("groups interface and implementation", () =>
{
    const string source =
        "diff --git a/src/Contracts/IManufacturerService.cs b/src/Contracts/IManufacturerService.cs\n@@ -0,0 +1,5 @@\n+public interface IManufacturerService\n+{\n+    List<string> GetManufacturerNames();\n+}\ndiff --git a/src/Services/ManufacturerService.cs b/src/Services/ManufacturerService.cs\n@@ -0,0 +1,5 @@\n+public sealed class ManufacturerService : IManufacturerService\n+{\n+    public List<string> GetManufacturerNames() => [];\n+}\n";
    var result = new LocalHeuristicDiffAnalysisService()
        .AnalyzeAsync(new DiffAnalysisRequestModel(".", DiffScopeModel.WorkingTree, source, "en")).GetAwaiter().GetResult();
    if (result.Groups.Count != 1 || !result.Groups[0].Description.Contains("contract") ||
        !result.Groups[0].Description.Contains("implementation"))
        throw new Exception("Expected interface and implementation group.");
});
Test("Codex provider maps every hunk", () =>
{
    var client = new FakeCodexCliClient([
        """{"groups":[{"title":"Processor","description":"Updates the shared processor.","hunkIds":["H0001","H0002"],"confidence":0.9,"reviewStatus":"good","reviewHint":null}]}"""
    ]);
    var result = new CodexCliDiffAnalysisAdapter(client, new LocalHeuristicDiffAnalysisService())
        .AnalyzeAsync(new DiffAnalysisRequestModel(".", DiffScopeModel.WorkingTree, diff, "en")).GetAwaiter().GetResult();
    if (result.Source != DiffAnalysisSourceModel.Codex || result.Groups.Count != 1 || result.Groups[0].Hunks.Count != 2 ||
        !client.Schemas[0].Contains("\"enum\"") ||
        !client.Schemas[0].Contains("H0002") || result.Groups[0].Review?.Status != DiffGroupReviewStatusModel.Good)
        throw new Exception("Expected validated Codex grouping with constrained ids.");
});
Test("Codex provider extracts JSON and normalizes hunk ids", () =>
{
    var client = new FakeCodexCliClient([
        """
        diagnostic line
        {"groups":[{"title":"Processor","description":"Updates the shared processor.","hunkIds":["h1","2"],"confidence":0.9,"reviewStatus":"ok","reviewHint":"Add a boundary test for null input."}]}
        """
    ]);
    var result = new CodexCliDiffAnalysisAdapter(client, new LocalHeuristicDiffAnalysisService())
        .AnalyzeAsync(new DiffAnalysisRequestModel(".", DiffScopeModel.WorkingTree, diff, "en")).GetAwaiter().GetResult();
    if (result.Source != DiffAnalysisSourceModel.Codex || result.Groups[0].Hunks.Count != 2 ||
        result.Groups[0].Review?.Hint != "Add a boundary test for null input.")
        throw new Exception("Expected tolerant final-message parsing.");
});
Test("invalid Codex assignment uses heuristic fallback", () =>
{
    var client = new FakeCodexCliClient([
        """{"groups":[{"title":"Invalid","description":"Duplicates a hunk.","hunkIds":["H0001","H0001"],"confidence":0.8,"reviewStatus":"good","reviewHint":null}]}"""
    ]);
    var result = new CodexCliDiffAnalysisAdapter(client, new LocalHeuristicDiffAnalysisService())
        .AnalyzeAsync(new DiffAnalysisRequestModel(".", DiffScopeModel.WorkingTree, diff, "en")).GetAwaiter().GetResult();
    if (result.Source != DiffAnalysisSourceModel.Heuristic || string.IsNullOrWhiteSpace(result.Diagnostic))
        throw new Exception("Expected visible heuristic fallback.");
});
Test("large Codex analysis merges chunk groups", () =>
{
    var client = new FakeCodexCliClient([
        """{"groups":[{"title":"First","description":"Updates the first part.","hunkIds":["H0001"],"confidence":0.8,"reviewStatus":"good","reviewHint":null}]}""",
        """{"groups":[{"title":"Second","description":"Updates the second part.","hunkIds":["H0002"],"confidence":0.8,"reviewStatus":"ok","reviewHint":"Add a regression test."}]}""",
        """{"groups":[{"title":"Processor","description":"Updates the shared processor.","groupIds":["G0001","G0002"],"confidence":0.9,"reviewStatus":"warning","reviewHint":"Validate processor input before use."}]}"""
    ]);
    var result = new CodexCliDiffAnalysisAdapter(client, new LocalHeuristicDiffAnalysisService(), 250)
        .AnalyzeAsync(new DiffAnalysisRequestModel(".", DiffScopeModel.WorkingTree, diff, "en")).GetAwaiter().GetResult();
    if (result.Source != DiffAnalysisSourceModel.Codex || result.Groups.Count != 1 || result.Groups[0].Hunks.Count != 2 ||
        client.Prompts.Count != 3 || result.Groups[0].Review?.Status != DiffGroupReviewStatusModel.Warning ||
        result.Groups[0].Review?.Hint != "Validate processor input before use.")
        throw new Exception("Expected lossless multi-stage grouping.");
});
Test("Codex provider rejects review status without required hint", () =>
{
    var client = new FakeCodexCliClient([
        """{"groups":[{"title":"Processor","description":"Updates the shared processor.","hunkIds":["H0001","H0002"],"confidence":0.9,"reviewStatus":"warning","reviewHint":null}]}"""
    ]);
    var result = new CodexCliDiffAnalysisAdapter(client, new LocalHeuristicDiffAnalysisService())
        .AnalyzeAsync(new DiffAnalysisRequestModel(".", DiffScopeModel.WorkingTree, diff, "en")).GetAwaiter().GetResult();
    if (result.Source != DiffAnalysisSourceModel.Heuristic || result.Groups.Any(group => group.Review is not null))
        throw new Exception("Expected invalid review to use the unreviewed heuristic fallback.");
});
Test("Codex client enforces Luna and safe execution", () =>
{
    var runner = new RecordingProcessRunner(new CodexProcessResultModel(0, "{}", string.Empty));
    var client = new CodexCliClientAdapter(runner);
    _ = client.RunStructuredAsync("prompt", "{}", TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
    var request = runner.LastRequest ?? throw new Exception("Expected process request.");
    if (!request.Arguments.Contains("gpt-6-luna") || !request.Arguments.Contains("read-only") ||
        !request.Arguments.Contains("approval_policy=\"never\"") ||
        !request.Arguments.Contains("web_search=\"disabled\"") ||
        !request.Arguments.Contains("--skip-git-repo-check") ||
        !request.Arguments.Contains("--output-last-message"))
        throw new Exception("Missing safe Codex arguments or final-message output.");
    if (!request.EnvironmentVariablesToRemove.Contains("OPENAI_API_KEY") ||
        !request.EnvironmentVariablesToRemove.Contains("CODEX_API_KEY") ||
        !request.EnvironmentVariablesToRemove.Contains("CODEX_ACCESS_TOKEN"))
        throw new Exception("Expected API credentials to be removed.");
    if (!runner.SchemaExistedDuringRun) throw new Exception("Expected output schema file.");
    if (!runner.SchemaWasValidJsonDuringRun || runner.SchemaHadUtf8BomDuringRun)
        throw new Exception("Expected BOM-free UTF-8 JSON schema.");
    if (request.Arguments.Contains("-C"))
        throw new Exception("Codex must not receive a repository working-directory argument.");
});
Test("protected diff files stay local", () =>
{
    const string mixed =
        "diff --git a/.env b/.env\n@@ -0,0 +1 @@\n+PASSWORD=secret\ndiff --git a/src/App.cs b/src/App.cs\n@@ -0,0 +1 @@\n+class App {}\n";
    var client = new FakeCodexCliClient([
        """{"groups":[{"title":"App","description":"Adds the app.","hunkIds":["H0001"],"confidence":0.9,"reviewStatus":"good","reviewHint":null}]}"""
    ]);
    var result = new CodexCliDiffAnalysisAdapter(client, new LocalHeuristicDiffAnalysisService())
        .AnalyzeAsync(new DiffAnalysisRequestModel("repo", DiffScopeModel.WorkingTree, mixed, "en")).GetAwaiter().GetResult();
    if (client.Prompts.Single().Contains("PASSWORD=secret") || result.Hunks.Count != 2 || result.Groups.Count < 2)
        throw new Exception("Protected files must remain in the local result only.");
});
Test("AI safety policy excludes sensitive binary and oversized changes", () =>
{
    var large = new string('x', AiDiffSafetyPolicy.MaximumRemoteHunkCharacters + 1);
    var source =
        $"diff --git a/.aws/credentials b/.aws/credentials\n@@ -0,0 +1 @@\n+SECRET=value\ndiff --git a/assets/logo.png b/assets/logo.png\nBinary files a/assets/logo.png and b/assets/logo.png differ\ndiff --git a/src/Large.cs b/src/Large.cs\n@@ -0,0 +1 @@\n+{large}\ndiff --git a/src/Safe.cs b/src/Safe.cs\n@@ -0,0 +1 @@\n+class Safe {{}}\n";
    var assessment = AiDiffSafetyPolicy.Assess(source, DiffScopeModel.WorkingTree);
    if (assessment.Preview.TransferableSections != 1 || assessment.Preview.ExcludedSections != 3 ||
        assessment.TransferableHunks.Single().FilePath != "src/Safe.cs")
        throw new Exception("Expected only safe bounded text to be transferable.");
});
Test("AI analysis preview matches the provider safety policy", () =>
{
    var diffs = new FakeDiffReader("", "diff --git a/.env b/.env\n@@ -0,0 +1 @@\n+TOKEN=secret\n", "");
    var provider = new RecordingAnalysisProvider();
    var preview = new DiffAnalysisService(diffs, provider, provider)
        .PreviewAiAsync(new DiffAnalysisQueryModel("repo", null, "en", true)).GetAwaiter().GetResult();
    if (preview.TotalSections != 1 || preview.TransferableSections != 0 || preview.ExcludedSections != 1)
        throw new Exception("Expected a transparent local-only preview.");
});
Test("Codex client rejects API key authentication", () =>
{
    var runner = new RecordingProcessRunner(new CodexProcessResultModel(0, "Logged in using an API key", string.Empty));
    var status = new CodexCliClientAdapter(runner).GetStatusAsync().GetAwaiter().GetResult();
    if (status.Kind != CodexCliStatusKindModel.ApiKeyAuthentication)
        throw new Exception("Expected API-key authentication rejection.");
});
Test("analysis use case combines staged and working diffs", () =>
{
    var diffs = new FakeDiffReader("staged", "working", "historical");
    var provider = new RecordingAnalysisProvider();
    var result = new DiffAnalysisService(diffs, provider, provider)
        .AnalyzeAsync(new DiffAnalysisQueryModel("repo", null, "en", true)).GetAwaiter().GetResult();
    if (!result.IsAiRequested || provider.Requests.Single().Diff != "stagedworking")
        throw new Exception("Expected combined current diff.");
});
Test("analysis use case loads historical diff only", () =>
{
    var diffs = new FakeDiffReader("staged", "working", "historical");
    var provider = new RecordingAnalysisProvider();
    _ = new DiffAnalysisService(diffs, provider, provider)
        .AnalyzeAsync(new DiffAnalysisQueryModel("repo", "abc", "de", false)).GetAwaiter().GetResult();
    if (provider.Requests.Single().Diff != "historical" || provider.Requests.Single().Scope != DiffScopeModel.Historical)
        throw new Exception("Expected historical diff.");
});
Test("diff renderer marks added removed and changed words", () =>
{
    var hunk = new DiffHunkModel("src/Foo.cs", DiffScopeModel.WorkingTree, "@@ -1 +1 @@",
        "-return oldValue;\n+return newValue;\n context", 1, 1);
    var page = DiffRenderer.CreatePage([hunk]);
    if (!page.Markers.Any(marker => marker.Kind == DiffHighlightKindModel.RemovedLine) ||
        !page.Markers.Any(marker => marker.Kind == DiffHighlightKindModel.AddedLine))
        throw new Exception("Expected line markers.");
    if (!page.Markers.Any(marker => marker.Kind == DiffHighlightKindModel.RemovedWord) ||
        !page.Markers.Any(marker => marker.Kind == DiffHighlightKindModel.AddedWord))
        throw new Exception("Expected word markers.");
});
Test("diff renderer maps real source line numbers to each diff line", () =>
{
    var hunk = new DiffHunkModel("src/Foo.cs", DiffScopeModel.WorkingTree, "@@ -4,2 +7,3 @@",
        "-old\n context\n+new", 4, 7);
    var page = DiffRenderer.CreatePage([hunk]);
    if (page.LineNumbers.Count != 4 || page.LineNumbers[1] != new DiffLineNumbersModel(4, null) ||
        page.LineNumbers[2] != new DiffLineNumbersModel(5, 7) || page.LineNumbers[3] != new DiffLineNumbersModel(null, 8))
        throw new Exception("Expected source line numbers beside diff lines.");
});
Test("diff renderer handles unicode word changes", () =>
{
    var hunk = new DiffHunkModel("src/Text.cs", DiffScopeModel.WorkingTree, "@@ -1 +1 @@",
        "-string title = \"Überblick\";\n+string title = \"Übersicht\";", 1, 1);
    var page = DiffRenderer.CreatePage([hunk]);
    if (!page.Markers.Any(marker => marker.Kind == DiffHighlightKindModel.RemovedWord) ||
        !page.Markers.Any(marker => marker.Kind == DiffHighlightKindModel.AddedWord))
        throw new Exception("Expected unicode word markers.");
});
Test("diff renderer pages large hunks without dropping content", () =>
{
    var content = string.Join('\n',
        Enumerable.Range(0, DiffRenderer.MaximumPageLines + 25).Select(index => $"+line {index}"));
    var hunk = new DiffHunkModel("src/Large.cs", DiffScopeModel.WorkingTree, "@@ -0,0 +1,2025 @@", content, 0, 1);
    var first = DiffRenderer.CreatePage([hunk]);
    if (first.NextPage is null || !first.Text.Contains("+line 0"))
        throw new Exception("Expected a first page and continuation.");
    var second = DiffRenderer.CreatePage([hunk], first.NextPage);
    if (!second.Text.Contains($"+line {DiffRenderer.MaximumPageLines - 1}") &&
        !second.Text.Contains($"+line {DiffRenderer.MaximumPageLines}"))
        throw new Exception("Expected remaining diff lines.");
});
Test("diff renderer bounds an initial 20000 line page", () =>
{
    var content = string.Join('\n', Enumerable.Range(0, 20_000).Select(index => $"+line {index}"));
    var page = DiffRenderer.CreatePage([
        new DiffHunkModel("src/Huge.cs", DiffScopeModel.WorkingTree, "@@ -0,0 +1,20000 @@", content, 0, 1)
    ]);
    if (page.NextPage is null || page.Text.Count(character => character == '\n') > DiffRenderer.MaximumPageLines)
        throw new Exception("Expected a bounded initial page.");
});
Test("render document preserves marker offsets when appending a page", () =>
{
    var first = new DiffRenderPageModel("+one\n", [new DiffTextMarkerModel(0, 4, DiffHighlightKindModel.AddedLine)],
        [new DiffLineNumbersModel(null, 1)], new DiffPageCursorModel(0, 1));
    var second = new DiffRenderPageModel("-two\n", [new DiffTextMarkerModel(0, 4, DiffHighlightKindModel.RemovedLine)],
        [new DiffLineNumbersModel(2, null)], null);
    var document = DiffRenderDocumentModel.FromPage(first).Append(second);
    if (document.Text != "+one\n-two\n" || document.Markers.Last().Offset != 5 ||
        document.LineNumbers.Last() != new DiffLineNumbersModel(2, null) || document.NextPage is not null)
        throw new Exception("Expected appended render document.");
});
Environment.ExitCode = failures.Count == 0 ? 0 : 1;

internal sealed class FakeDiffReader(string staged, string working, string historical) : IRepositoryDiffReaderPort
{
    public Task<string> GetDiffAsync(string repositoryPath, DiffScopeModel scope, string? path = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(scope == DiffScopeModel.Staged ? staged : working);
    }

    public Task<string> GetCommitDiffAsync(string repositoryPath, string commitId, string? path = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(historical);
    }
}

internal sealed class RecordingAnalysisProvider : IDiffAnalysisProviderPort
{
    public List<DiffAnalysisRequestModel> Requests { get; } = [];

    public Task<DiffAnalysisResultModel> AnalyzeAsync(DiffAnalysisRequestModel request,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(new DiffAnalysisResultModel([], [], DiffAnalysisSourceModel.Heuristic));
    }
}

internal sealed class FakeCodexCliClient(IEnumerable<string> responses) : ICodexSessionClientPort, ICodexStructuredRunnerPort
{
    private readonly Queue<string> _responses = new(responses);
    public List<string> Prompts { get; } = [];
    public List<string> Schemas { get; } = [];

    public Task<CodexCliStatusModel> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CodexCliStatusModel(CodexCliStatusKindModel.Ready, "Ready"));
    }

    public Task<CodexCliStatusModel> LoginAsync(CancellationToken cancellationToken = default)
    {
        return GetStatusAsync(cancellationToken);
    }

    public Task<string> RunStructuredAsync(string prompt, string schema, TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        Prompts.Add(prompt);
        Schemas.Add(schema);
        return Task.FromResult(_responses.Dequeue());
    }
}

internal sealed class RecordingProcessRunner(CodexProcessResultModel result) : ICodexProcessRunnerPort
{
    public CodexProcessRequestModel? LastRequest { get; private set; }
    public bool SchemaExistedDuringRun { get; private set; }
    public bool SchemaWasValidJsonDuringRun { get; private set; }
    public bool SchemaHadUtf8BomDuringRun { get; private set; }

    public Task<CodexProcessResultModel> RunAsync(CodexProcessRequestModel request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        var schemaIndex = -1;
        for (var index = 0; index < request.Arguments.Count; index++)
            if (request.Arguments[index] == "--output-schema")
            {
                schemaIndex = index;
                break;
            }

        SchemaExistedDuringRun = schemaIndex >= 0 && schemaIndex + 1 < request.Arguments.Count &&
                                 File.Exists(request.Arguments[schemaIndex + 1]);
        if (SchemaExistedDuringRun)
        {
            var bytes = File.ReadAllBytes(request.Arguments[schemaIndex + 1]);
            SchemaHadUtf8BomDuringRun = bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF });
            try
            {
                using var document = JsonDocument.Parse(bytes);
                SchemaWasValidJsonDuringRun = document.RootElement.ValueKind == JsonValueKind.Object;
            }
            catch (JsonException)
            {
            }
        }

        return Task.FromResult(result);
    }
}
