using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.Abstracts.Parsers;
using GrimmDiff.Logic.Abstracts.Ports;
using static GrimmDiff.Logic.Processors.DiffAnalysis.HeuristicFeatureExtractor;

namespace GrimmDiff.Logic.Processors.DiffAnalysis;

public sealed class LocalHeuristicDiffAnalysisService : IDiffAnalysisProviderPort
{
    public Task<DiffAnalysisResultModel> AnalyzeAsync(DiffAnalysisRequestModel request,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hunks = UnifiedDiffParser.Parse(request.Diff, request.Scope);
            var features = new List<Features>(hunks.Count);
            foreach (var hunk in hunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                features.Add(CreateFeatures(hunk));
            }

            var clusters = BuildClusters(features, cancellationToken);
            var groups = clusters.Select(cluster => ToGroup(cluster, request.Language)).ToList();
            return new DiffAnalysisResultModel(hunks, groups, DiffAnalysisSourceModel.Heuristic);
        }, cancellationToken);
    }

    #region Private

    private static List<List<Features>> BuildClusters(List<Features> features, CancellationToken cancellationToken)
    {
        var clusters = features.Select(f => new List<Features> { f }).ToList();
        for (var i = 0; i < clusters.Count; i++)
        for (var j = clusters.Count - 1; j > i; j--)
        {
            if ((j & 31) == 0) cancellationToken.ThrowIfCancellationRequested();
            if (clusters[i].Any(left => clusters[j].Any(right => Similarity(left, right) >= 0.35)))
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

        // A declaration referenced by another changed file is a strong offline signal.
        if (left.Declarations.Any(x => right.Identifiers.Contains(x.Name)) ||
            right.Declarations.Any(x => left.Identifiers.Contains(x.Name))) return 1;

        // IOrderService.cs and OrderService.cs describe one concept even without a direct reference in the hunk.
        if (left.Concepts.Overlaps(right.Concepts)) return 0.85;

        var score = left.Directory.Equals(right.Directory, StringComparison.OrdinalIgnoreCase) ? 0.25 : 0;
        score += Jaccard(left.Tokens, right.Tokens) * 0.45;
        return score;
    }

    private static WorkStepGroupModel ToGroup(List<Features> cluster, string language)
    {
        var german = language.StartsWith("de", StringComparison.OrdinalIgnoreCase);
        var declarations = cluster.SelectMany(x => x.Declarations).ToList();
        var interfaces = declarations.Where(x => x.Kind.Equals("interface", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var implementations = declarations.Where(x => x.Kind.Equals("class", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var subject = SelectSubject(cluster, interfaces, implementations);
        var methods = cluster.SelectMany(x => x.Methods).Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList();
        var files = cluster.Select(x => x.Hunk.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var title = german ? $"Änderung: {subject}" : $"Change: {subject}";
        var isAddition = cluster.All(x => x.HasAdditions && !x.HasRemovals);
        var description = Describe(german, interfaces, implementations, methods, files, isAddition);
        var confidence = cluster.Count == 1 ? 0.55 : 0.90;
        return new WorkStepGroupModel(title, description, cluster.Select(x => x.Hunk).ToList(), confidence);
    }

    private static string SelectSubject(List<Features> cluster, List<string> interfaces, List<string> implementations)
    {
        if (interfaces.Count > 0) return interfaces[0];
        if (implementations.Count > 0) return implementations[0];
        return cluster.SelectMany(x => x.Concepts).GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                   .OrderByDescending(x => x.Count()).ThenByDescending(x => x.Key.Length).FirstOrDefault()?.Key
               ?? Path.GetFileNameWithoutExtension(cluster[0].Hunk.FilePath);
    }

    private static string Describe(bool german, List<string> interfaces, List<string> implementations,
        List<string> methods, List<string> files, bool isAddition)
    {
        if (interfaces.Count > 0 && implementations.Count > 0)
            return german
                ? isAddition
                    ? $"Fügt den Vertrag {interfaces[0]} und die Implementierung {implementations[0]} hinzu."
                    : $"Ändert den Vertrag {interfaces[0]} und die Implementierung {implementations[0]}."
                : isAddition
                    ? $"Adds the {interfaces[0]} contract and its {implementations[0]} implementation."
                    : $"Updates the {interfaces[0]} contract and its {implementations[0]} implementation.";
        if (interfaces.Count > 0)
        {
            var operations = Operations(methods);
            return german
                ? isAddition
                    ? $"Fügt den Vertrag {interfaces[0]}{operations} hinzu."
                    : $"Ändert den Vertrag {interfaces[0]}{operations}."
                : isAddition
                    ? $"Adds the {interfaces[0]} contract{operations}."
                    : $"Updates the {interfaces[0]} contract{operations}.";
        }

        if (implementations.Count > 0)
        {
            var operations = Operations(methods);
            return german
                ? isAddition
                    ? $"Fügt die Implementierung {implementations[0]}{operations} hinzu."
                    : $"Ändert die Implementierung {implementations[0]}{operations}."
                : isAddition
                    ? $"Adds the {implementations[0]} implementation{operations}."
                    : $"Updates the {implementations[0]} implementation{operations}.";
        }

        var fileName = Path.GetFileName(files[0]);
        return german
            ? files.Count == 1
                ? $"Ändert die Konfiguration in {fileName}."
                : $"Ändert zusammengehörige Konfigurationen in {files.Count} Dateien."
            : files.Count == 1
                ? $"Updates configuration in {fileName}."
                : $"Updates related configuration in {files.Count} files.";
    }

    private static string Operations(List<string> methods)
    {
        return methods.Count == 0 ? string.Empty : $" ({string.Join(", ", methods)})";
    }

    private static double Jaccard(HashSet<string> a, HashSet<string> b)
    {
        return a.Count == 0 || b.Count == 0
            ? 0
            : (double)a.Intersect(b, StringComparer.OrdinalIgnoreCase).Count() /
              a.Union(b, StringComparer.OrdinalIgnoreCase).Count();
    }

    #endregion
}