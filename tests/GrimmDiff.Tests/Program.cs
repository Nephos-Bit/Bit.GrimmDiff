using GrimmDiff.Domain;
using GrimmDiff.Application;
using GrimmDiff.Infrastructure;
using GrimmDiff.App;
using System.IO;

var failures = new List<string>();
void Test(string name, Action action) { try { action(); Console.WriteLine($"PASS {name}"); } catch (Exception ex) { failures.Add($"{name}: {ex.Message}"); Console.Error.WriteLine($"FAIL {name}: {ex.Message}"); } }

const string diff = "diff --git a/src/Foo.cs b/src/Foo.cs\nindex 123..456 100644\n--- a/src/Foo.cs\n+++ b/src/Foo.cs\n@@ -4,2 +4,3 @@ class Foo\n-  void Run() {}\n+  // SharedProcessor process data\n+  void ProcessData() {}\ndiff --git a/src/Bar.cs b/src/Bar.cs\nindex 123..456 100644\n--- a/src/Bar.cs\n+++ b/src/Bar.cs\n@@ -1 +1 @@\n-// old\n+// SharedProcessor process data\n";
Test("parser returns hunks", () => { var hunks = UnifiedDiffParser.Parse(diff, DiffScope.WorkingTree); if (hunks.Count != 2 || hunks[0].FilePath != "src/Foo.cs") throw new Exception("Unexpected hunk parse result."); });
Test("parser retains file changes without text hunks", () => { const string source = "diff --git a/assets/logo.png b/assets/logo.png\nnew file mode 100644\nindex 0000000..1234567\nBinary files /dev/null and b/assets/logo.png differ\n"; var hunks = UnifiedDiffParser.Parse(source, DiffScope.WorkingTree); if (hunks.Count != 1 || hunks[0].Header != "File metadata" || !hunks[0].Content.Contains("Binary files")) throw new Exception("Expected binary file change."); });
Test("heuristic groups related text", () => { var result = new LocalHeuristicDiffAnalysisProvider().AnalyzeAsync(new DiffAnalysisRequest(".", DiffScope.WorkingTree, diff, "en")).GetAwaiter().GetResult(); if (result.Groups.Count != 1) throw new Exception("Expected semantic group."); });
Test("unrelated hunks fall back to separate groups", () => { const string source = "diff --git a/a.cs b/a.cs\n@@ -1 +1 @@\n+// network transport\ndiff --git a/b.cs b/b.cs\n@@ -1 +1 @@\n+// invoice ledger\n"; var result = new LocalHeuristicDiffAnalysisProvider().AnalyzeAsync(new DiffAnalysisRequest(".", DiffScope.WorkingTree, source, "de")).GetAwaiter().GetResult(); if (result.Groups.Count < 2) throw new Exception("Expected fallback grouping."); });
Test("groups interface and implementation", () => { const string source = "diff --git a/src/Contracts/IManufacturerService.cs b/src/Contracts/IManufacturerService.cs\n@@ -0,0 +1,5 @@\n+public interface IManufacturerService\n+{\n+    List<string> GetManufacturerNames();\n+}\ndiff --git a/src/Services/ManufacturerService.cs b/src/Services/ManufacturerService.cs\n@@ -0,0 +1,5 @@\n+public sealed class ManufacturerService : IManufacturerService\n+{\n+    public List<string> GetManufacturerNames() => [];\n+}\n"; var result = new LocalHeuristicDiffAnalysisProvider().AnalyzeAsync(new DiffAnalysisRequest(".", DiffScope.WorkingTree, source, "en")).GetAwaiter().GetResult(); if (result.Groups.Count != 1 || !result.Groups[0].Description.Contains("contract") || !result.Groups[0].Description.Contains("implementation")) throw new Exception("Expected interface and implementation group."); });
Test("Codex provider maps every hunk", () =>
{
    var client = new FakeCodexCliClient(["""{"groups":[{"title":"Processor","description":"Updates the shared processor.","hunkIds":["H0001","H0002"],"confidence":0.9}]}"""]);
    var result = new CodexCliDiffAnalysisProvider(client, new LocalHeuristicDiffAnalysisProvider()).AnalyzeAsync(new DiffAnalysisRequest(".", DiffScope.WorkingTree, diff, "en")).GetAwaiter().GetResult();
    if (result.Source != DiffAnalysisSource.Codex || result.Groups.Count != 1 || result.Groups[0].Hunks.Count != 2 || !client.Schemas[0].Contains("\"enum\"") || !client.Schemas[0].Contains("H0002")) throw new Exception("Expected validated Codex grouping with constrained ids.");
});
Test("Codex provider extracts JSON and normalizes hunk ids", () =>
{
    var client = new FakeCodexCliClient(["""
        diagnostic line
        {"groups":[{"title":"Processor","description":"Updates the shared processor.","hunkIds":["h1","2"],"confidence":0.9}]}
        """]);
    var result = new CodexCliDiffAnalysisProvider(client, new LocalHeuristicDiffAnalysisProvider()).AnalyzeAsync(new DiffAnalysisRequest(".", DiffScope.WorkingTree, diff, "en")).GetAwaiter().GetResult();
    if (result.Source != DiffAnalysisSource.Codex || result.Groups[0].Hunks.Count != 2) throw new Exception("Expected tolerant final-message parsing.");
});
Test("invalid Codex assignment uses heuristic fallback", () =>
{
    var client = new FakeCodexCliClient(["""{"groups":[{"title":"Invalid","description":"Duplicates a hunk.","hunkIds":["H0001","H0001"],"confidence":0.8}]}"""]);
    var result = new CodexCliDiffAnalysisProvider(client, new LocalHeuristicDiffAnalysisProvider()).AnalyzeAsync(new DiffAnalysisRequest(".", DiffScope.WorkingTree, diff, "en")).GetAwaiter().GetResult();
    if (result.Source != DiffAnalysisSource.Heuristic || string.IsNullOrWhiteSpace(result.Diagnostic)) throw new Exception("Expected visible heuristic fallback.");
});
Test("large Codex analysis merges chunk groups", () =>
{
    var client = new FakeCodexCliClient([
        """{"groups":[{"title":"First","description":"Updates the first part.","hunkIds":["H0001"],"confidence":0.8}]}""",
        """{"groups":[{"title":"Second","description":"Updates the second part.","hunkIds":["H0002"],"confidence":0.8}]}""",
        """{"groups":[{"title":"Processor","description":"Updates the shared processor.","groupIds":["G0001","G0002"],"confidence":0.9}]}"""]);
    var result = new CodexCliDiffAnalysisProvider(client, new LocalHeuristicDiffAnalysisProvider(), 250).AnalyzeAsync(new DiffAnalysisRequest(".", DiffScope.WorkingTree, diff, "en")).GetAwaiter().GetResult();
    if (result.Source != DiffAnalysisSource.Codex || result.Groups.Count != 1 || result.Groups[0].Hunks.Count != 2 || client.Prompts.Count != 3) throw new Exception("Expected lossless multi-stage grouping.");
});
Test("Codex client enforces Luna and safe execution", () =>
{
    var runner = new RecordingProcessRunner(new CodexProcessResult(0, "{}", string.Empty));
    var client = new CodexCliClient(runner);
    _ = client.RunStructuredAsync("prompt", "{}", TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
    var request = runner.LastRequest ?? throw new Exception("Expected process request.");
    if (!request.Arguments.Contains("gpt-6-luna") || !request.Arguments.Contains("read-only") || !request.Arguments.Contains("approval_policy=\"never\"") || !request.Arguments.Contains("web_search=\"disabled\"") || !request.Arguments.Contains("--skip-git-repo-check") || !request.Arguments.Contains("--output-last-message")) throw new Exception("Missing safe Codex arguments or final-message output.");
    if (!request.EnvironmentVariablesToRemove.Contains("OPENAI_API_KEY") || !request.EnvironmentVariablesToRemove.Contains("CODEX_API_KEY") || !request.EnvironmentVariablesToRemove.Contains("CODEX_ACCESS_TOKEN")) throw new Exception("Expected API credentials to be removed.");
    if (!runner.SchemaExistedDuringRun) throw new Exception("Expected output schema file.");
    if (!runner.SchemaWasValidJsonDuringRun || runner.SchemaHadUtf8BomDuringRun) throw new Exception("Expected BOM-free UTF-8 JSON schema.");
    if (request.Arguments.Contains("-C")) throw new Exception("Codex must not receive a repository working-directory argument.");
});
Test("protected diff files stay local", () =>
{
    const string mixed = "diff --git a/.env b/.env\n@@ -0,0 +1 @@\n+PASSWORD=secret\ndiff --git a/src/App.cs b/src/App.cs\n@@ -0,0 +1 @@\n+class App {}\n";
    var client = new FakeCodexCliClient(["""{"groups":[{"title":"App","description":"Adds the app.","hunkIds":["H0001"],"confidence":0.9}]}"""]);
    var result = new CodexCliDiffAnalysisProvider(client, new LocalHeuristicDiffAnalysisProvider()).AnalyzeAsync(new DiffAnalysisRequest("repo", DiffScope.WorkingTree, mixed, "en")).GetAwaiter().GetResult();
    if (client.Prompts.Single().Contains("PASSWORD=secret") || result.Hunks.Count != 2 || result.Groups.Count < 2) throw new Exception("Protected files must remain in the local result only.");
});
Test("Codex client rejects API key authentication", () =>
{
    var runner = new RecordingProcessRunner(new CodexProcessResult(0, "Logged in using an API key", string.Empty));
    var status = new CodexCliClient(runner).GetStatusAsync().GetAwaiter().GetResult();
    if (status.Kind != CodexCliStatusKind.ApiKeyAuthentication) throw new Exception("Expected API-key authentication rejection.");
});
Test("analysis use case combines staged and working diffs", () =>
{
    var diffs = new FakeDiffReader("staged", "working", "historical");
    var provider = new RecordingAnalysisProvider();
    var result = new DiffAnalysisService(diffs, provider, provider).AnalyzeAsync(new DiffAnalysisQuery("repo", null, "en", true)).GetAwaiter().GetResult();
    if (!result.IsAiRequested || provider.Requests.Single().Diff != "stagedworking") throw new Exception("Expected combined current diff.");
});
Test("analysis use case loads historical diff only", () =>
{
    var diffs = new FakeDiffReader("staged", "working", "historical");
    var provider = new RecordingAnalysisProvider();
    _ = new DiffAnalysisService(diffs, provider, provider).AnalyzeAsync(new DiffAnalysisQuery("repo", "abc", "de", false)).GetAwaiter().GetResult();
    if (provider.Requests.Single().Diff != "historical" || provider.Requests.Single().Scope != DiffScope.Historical) throw new Exception("Expected historical diff.");
});
Test("diff renderer marks added removed and changed words", () =>
{
    var hunk = new DiffHunk("src/Foo.cs", DiffScope.WorkingTree, "@@ -1 +1 @@", "-return oldValue;\n+return newValue;\n context", 1, 1);
    var page = DiffRenderBuilder.CreatePage([hunk]);
    if (!page.Markers.Any(marker => marker.Kind == DiffHighlightKind.RemovedLine) || !page.Markers.Any(marker => marker.Kind == DiffHighlightKind.AddedLine)) throw new Exception("Expected line markers.");
    if (!page.Markers.Any(marker => marker.Kind == DiffHighlightKind.RemovedWord) || !page.Markers.Any(marker => marker.Kind == DiffHighlightKind.AddedWord)) throw new Exception("Expected word markers.");
});
Test("diff renderer handles unicode word changes", () =>
{
    var hunk = new DiffHunk("src/Text.cs", DiffScope.WorkingTree, "@@ -1 +1 @@", "-string title = \"Überblick\";\n+string title = \"Übersicht\";", 1, 1);
    var page = DiffRenderBuilder.CreatePage([hunk]);
    if (!page.Markers.Any(marker => marker.Kind == DiffHighlightKind.RemovedWord) || !page.Markers.Any(marker => marker.Kind == DiffHighlightKind.AddedWord)) throw new Exception("Expected unicode word markers.");
});
Test("diff renderer pages large hunks without dropping content", () =>
{
    var content = string.Join('\n', Enumerable.Range(0, DiffRenderBuilder.MaximumPageLines + 25).Select(index => $"+line {index}"));
    var hunk = new DiffHunk("src/Large.cs", DiffScope.WorkingTree, "@@ -0,0 +1,2025 @@", content, 0, 1);
    var first = DiffRenderBuilder.CreatePage([hunk]);
    if (first.NextPage is null || !first.Text.Contains("+line 0")) throw new Exception("Expected a first page and continuation.");
    var second = DiffRenderBuilder.CreatePage([hunk], first.NextPage);
    if (!second.Text.Contains($"+line {DiffRenderBuilder.MaximumPageLines - 1}") && !second.Text.Contains($"+line {DiffRenderBuilder.MaximumPageLines}")) throw new Exception("Expected remaining diff lines.");
});
Test("diff renderer bounds an initial 20000 line page", () =>
{
    var content = string.Join('\n', Enumerable.Range(0, 20_000).Select(index => $"+line {index}"));
    var page = DiffRenderBuilder.CreatePage([new DiffHunk("src/Huge.cs", DiffScope.WorkingTree, "@@ -0,0 +1,20000 @@", content, 0, 1)]);
    if (page.NextPage is null || page.Text.Count(character => character == '\n') > DiffRenderBuilder.MaximumPageLines) throw new Exception("Expected a bounded initial page.");
});
Test("render document preserves marker offsets when appending a page", () =>
{
    var first = new DiffRenderPage("+one\n", [new DiffTextMarker(0, 4, DiffHighlightKind.AddedLine)], new DiffPageCursor(0, 1));
    var second = new DiffRenderPage("-two\n", [new DiffTextMarker(0, 4, DiffHighlightKind.RemovedLine)], null);
    var document = DiffRenderDocument.FromPage(first).Append(second);
    if (document.Text != "+one\n-two\n" || document.Markers.Last().Offset != 5 || document.NextPage is not null) throw new Exception("Expected appended render document.");
});
Environment.ExitCode = failures.Count == 0 ? 0 : 1;

sealed class FakeDiffReader(string staged, string working, string historical) : IRepositoryDiffReader
{
    public Task<string> GetDiffAsync(string repositoryPath, DiffScope scope, string? path = null, CancellationToken cancellationToken = default) => Task.FromResult(scope == DiffScope.Staged ? staged : working);
    public Task<string> GetCommitDiffAsync(string repositoryPath, string commitId, string? path = null, CancellationToken cancellationToken = default) => Task.FromResult(historical);
}

sealed class RecordingAnalysisProvider : IDiffAnalysisProvider
{
    public List<DiffAnalysisRequest> Requests { get; } = [];
    public Task<DiffAnalysisResult> AnalyzeAsync(DiffAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(new DiffAnalysisResult([], [], DiffAnalysisSource.Heuristic));
    }
}

sealed class FakeCodexCliClient(IEnumerable<string> responses) : ICodexCliClient
{
    private readonly Queue<string> _responses = new(responses);
    public List<string> Prompts { get; } = [];
    public List<string> Schemas { get; } = [];
    public Task<CodexCliStatus> GetStatusAsync(CancellationToken cancellationToken = default) => Task.FromResult(new CodexCliStatus(CodexCliStatusKind.Ready, "Ready"));
    public Task<CodexCliStatus> LoginAsync(CancellationToken cancellationToken = default) => GetStatusAsync(cancellationToken);
    public Task<string> RunStructuredAsync(string prompt, string schema, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        Prompts.Add(prompt);
        Schemas.Add(schema);
        return Task.FromResult(_responses.Dequeue());
    }
}

sealed class RecordingProcessRunner(CodexProcessResult result) : ICodexProcessRunner
{
    public CodexProcessRequest? LastRequest { get; private set; }
    public bool SchemaExistedDuringRun { get; private set; }
    public bool SchemaWasValidJsonDuringRun { get; private set; }
    public bool SchemaHadUtf8BomDuringRun { get; private set; }
    public Task<CodexProcessResult> RunAsync(CodexProcessRequest request, CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        var schemaIndex = -1;
        for (var index = 0; index < request.Arguments.Count; index++)
            if (request.Arguments[index] == "--output-schema") { schemaIndex = index; break; }
        SchemaExistedDuringRun = schemaIndex >= 0 && schemaIndex + 1 < request.Arguments.Count && File.Exists(request.Arguments[schemaIndex + 1]);
        if (SchemaExistedDuringRun)
        {
            var bytes = File.ReadAllBytes(request.Arguments[schemaIndex + 1]);
            SchemaHadUtf8BomDuringRun = bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF });
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(bytes);
                SchemaWasValidJsonDuringRun = document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object;
            }
            catch (System.Text.Json.JsonException) { }
        }
        return Task.FromResult(result);
    }
}
