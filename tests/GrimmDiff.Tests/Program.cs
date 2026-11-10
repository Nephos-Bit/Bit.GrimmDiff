using GrimmDiff.Domain;
using GrimmDiff.Infrastructure;

var failures = new List<string>();
void Test(string name, Action action) { try { action(); Console.WriteLine($"PASS {name}"); } catch (Exception ex) { failures.Add($"{name}: {ex.Message}"); Console.Error.WriteLine($"FAIL {name}: {ex.Message}"); } }

const string diff = "diff --git a/src/Foo.cs b/src/Foo.cs\nindex 123..456 100644\n--- a/src/Foo.cs\n+++ b/src/Foo.cs\n@@ -4,2 +4,3 @@ class Foo\n-  void Run() {}\n+  // process data\n+  void ProcessData() {}\ndiff --git a/src/Bar.cs b/src/Bar.cs\nindex 123..456 100644\n--- a/src/Bar.cs\n+++ b/src/Bar.cs\n@@ -1 +1 @@\n-// old\n+// process data\n";
Test("parser returns hunks", () => { var hunks = UnifiedDiffParser.Parse(diff, DiffScope.WorkingTree); if (hunks.Count != 2 || hunks[0].FilePath != "src/Foo.cs") throw new Exception("Unexpected hunk parse result."); });
Test("heuristic groups related text", () => { var result = new LocalHeuristicDiffAnalysisProvider().AnalyzeAsync(new DiffAnalysisRequest(DiffScope.WorkingTree, diff, "en")).GetAwaiter().GetResult(); if (result.Groups.Count != 1 || !result.Groups[0].Description.Contains("2 files")) throw new Exception("Expected semantic group."); });
Test("unrelated hunks fall back to separate groups", () => { const string source = "diff --git a/a.cs b/a.cs\n@@ -1 +1 @@\n+// network transport\ndiff --git a/b.cs b/b.cs\n@@ -1 +1 @@\n+// invoice ledger\n"; var result = new LocalHeuristicDiffAnalysisProvider().AnalyzeAsync(new DiffAnalysisRequest(DiffScope.WorkingTree, source, "de")).GetAwaiter().GetResult(); if (result.Groups.Count < 2) throw new Exception("Expected fallback grouping."); });
Environment.ExitCode = failures.Count == 0 ? 0 : 1;
