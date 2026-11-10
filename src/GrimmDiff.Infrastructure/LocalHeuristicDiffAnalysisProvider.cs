using System.Text.RegularExpressions;
using GrimmDiff.Domain;

namespace GrimmDiff.Infrastructure;

public sealed partial class LocalHeuristicDiffAnalysisProvider : IDiffAnalysisProvider
{
    [GeneratedRegex("(?:class|interface|struct|enum|record|void|async|public|private|protected|internal|function|def)\\s+([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.IgnoreCase)]
    private static partial Regex SymbolPattern();
    [GeneratedRegex("[A-Za-z_][A-Za-z0-9_]{2,}")]
    private static partial Regex TokenPattern();
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    { "public", "private", "protected", "internal", "static", "class", "string", "void", "return", "using", "namespace", "this", "with", "from", "that", "function", "async", "await", "const", "readonly", "null", "true", "false" };

    public Task<DiffAnalysisResult> AnalyzeAsync(DiffAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        var hunks = UnifiedDiffParser.Parse(request.Diff, request.Scope);
        var features = hunks.Select(h => new Features(h, Tokens(h), Symbols(h), Directory(h.FilePath))).ToList();
        var clusters = BuildClusters(features);
        var groups = clusters.Select(cluster => ToGroup(cluster, request.Language)).ToList();
        return Task.FromResult(new DiffAnalysisResult(hunks, groups));
    }

    private static List<List<Features>> BuildClusters(List<Features> features)
    {
        var clusters = features.Select(f => new List<Features> { f }).ToList();
        for (var i = 0; i < clusters.Count; i++)
        for (var j = clusters.Count - 1; j > i; j--)
        {
            if (clusters[i].Any(left => clusters[j].Any(right => Similarity(left, right) >= 0.40)))
            {
                clusters[i].AddRange(clusters[j]);
                clusters.RemoveAt(j);
            }
        }
        return clusters;
    }

    private static double Similarity(Features left, Features right)
    {
        if (left.Hunk.FilePath.Equals(right.Hunk.FilePath, StringComparison.OrdinalIgnoreCase)) return 0.95;
        var score = left.Directory.Equals(right.Directory, StringComparison.OrdinalIgnoreCase) ? 0.25 : 0;
        score += Jaccard(left.Symbols, right.Symbols) * 0.45;
        score += Jaccard(left.Tokens, right.Tokens) * 0.45;
        return score;
    }

    private static WorkStepGroup ToGroup(List<Features> cluster, string language)
    {
        var sharedSymbols = cluster.SelectMany(x => x.Symbols).GroupBy(x => x, StringComparer.OrdinalIgnoreCase).OrderByDescending(x => x.Count()).FirstOrDefault()?.Key;
        var representative = sharedSymbols ?? cluster.SelectMany(x => x.Tokens).GroupBy(x => x, StringComparer.OrdinalIgnoreCase).OrderByDescending(x => x.Count()).FirstOrDefault()?.Key ?? Path.GetFileNameWithoutExtension(cluster[0].Hunk.FilePath);
        var files = cluster.Select(x => x.Hunk.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var german = language.StartsWith("de", StringComparison.OrdinalIgnoreCase);
        var title = german ? $"Änderung: {representative}" : $"Change: {representative}";
        var description = german
            ? (files.Count == 1 ? $"Bearbeitet {files[0]} im Zusammenhang mit {representative}." : $"Verbindet {files.Count} Dateien im Zusammenhang mit {representative}.")
            : (files.Count == 1 ? $"Updates {files[0]} related to {representative}." : $"Connects {files.Count} files related to {representative}.");
        var confidence = cluster.Count == 1 ? 0.45 : 0.75;
        return new WorkStepGroup(title, description, cluster.Select(x => x.Hunk).ToList(), confidence);
    }

    private static HashSet<string> Tokens(DiffHunk hunk) => TokenPattern().Matches(hunk.Content)
        .Select(m => m.Value).Where(x => !StopWords.Contains(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
    private static HashSet<string> Symbols(DiffHunk hunk) => SymbolPattern().Matches(hunk.Content).Select(m => m.Groups[1].Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
    private static string Directory(string path) => Path.GetDirectoryName(path)?.Replace('\\', '/') ?? string.Empty;
    private static double Jaccard(HashSet<string> a, HashSet<string> b) => a.Count == 0 || b.Count == 0 ? 0 : (double)a.Intersect(b, StringComparer.OrdinalIgnoreCase).Count() / a.Union(b, StringComparer.OrdinalIgnoreCase).Count();
    private sealed record Features(DiffHunk Hunk, HashSet<string> Tokens, HashSet<string> Symbols, string Directory);
}
