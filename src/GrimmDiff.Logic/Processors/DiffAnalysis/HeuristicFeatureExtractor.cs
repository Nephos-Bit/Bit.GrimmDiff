using System.Text.RegularExpressions;
using GrimmDiff.Logic.Abstracts.Models;

namespace GrimmDiff.Logic.Processors.DiffAnalysis;

internal static partial class HeuristicFeatureExtractor
{    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "public", "private", "protected", "internal", "static", "class", "interface", "struct", "enum", "record",
        "string", "void", "return", "using", "namespace", "this", "with", "from", "that", "function", "async",
        "await", "const", "readonly", "null", "true", "false", "new", "get", "set", "list", "task", "var"
    };

    [GeneratedRegex("\\b(?<kind>class|interface|struct|enum|record)\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.IgnoreCase)]
    private static partial Regex DeclarationPattern();

    [GeneratedRegex("[A-Za-z_][A-Za-z0-9_]{2,}")]
    private static partial Regex TokenPattern();

    [GeneratedRegex(
        "\\b(?:public|private|protected|internal)\\s+(?:async\\s+)?[A-Za-z_][A-Za-z0-9_<>,.\\[\\]?]*\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s*\\(",
        RegexOptions.IgnoreCase)]
    private static partial Regex MethodPattern();

    internal static Features CreateFeatures(DiffHunkModel hunk)
    {
        var declarations = DeclarationPattern().Matches(hunk.Content)
            .Select(match => new Declaration(match.Groups["kind"].Value, match.Groups["name"].Value))
            .ToList();
        var identifiers = Identifiers(hunk.Content);
        var fileSubject = Path.GetFileNameWithoutExtension(hunk.FilePath);
        var concepts = declarations.Select(x => NormaliseConcept(x.Name))
            .Append(NormaliseConcept(fileSubject))
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var methods = MethodPattern().Matches(hunk.Content).Select(x => x.Groups["name"].Value)
            .Where(x => !StopWords.Contains(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var lines = hunk.Content.Split('\n');
        var hasAdditions = lines.Any(x => x.StartsWith('+') && !x.StartsWith("+++", StringComparison.Ordinal));
        var hasRemovals = lines.Any(x => x.StartsWith('-') && !x.StartsWith("---", StringComparison.Ordinal));
        return new Features(hunk, Tokens(hunk), declarations, identifiers, concepts, methods, hasAdditions, hasRemovals,
            Directory(hunk.FilePath));
    }

    private static HashSet<string> Tokens(DiffHunkModel hunk)
    {
        return Identifiers(hunk.Content);
    }

    private static HashSet<string> Identifiers(string content)
    {
        return TokenPattern().Matches(content).Select(m => m.Value).Where(x => !StopWords.Contains(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string NormaliseConcept(string value)
    {
        return value.Length > 2 && value[0] == 'I' && char.IsUpper(value[1]) ? value[1..] : value;
    }

    private static string Directory(string path)
    {
        return Path.GetDirectoryName(path)?.Replace('\\', '/') ?? string.Empty;
    }

    internal sealed record Declaration(string Kind, string Name);

    internal sealed record Features(
        DiffHunkModel Hunk,
        HashSet<string> Tokens,
        List<Declaration> Declarations,
        HashSet<string> Identifiers,
        HashSet<string> Concepts,
        HashSet<string> Methods,
        bool HasAdditions,
        bool HasRemovals,
        string Directory);

}
