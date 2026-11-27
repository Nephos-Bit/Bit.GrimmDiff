using System.Collections.Concurrent;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GrimmDiff.Data.Exceptions;
using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.Abstracts.Policies;
using GrimmDiff.Logic.Abstracts.Ports;
using static GrimmDiff.Data.Adapters.Codex.CodexAnalysisProtocol;

namespace GrimmDiff.Data.Adapters.Codex;

public sealed class CodexCliDiffAnalysisAdapter : IDiffAnalysisProviderPort
{
    public const int DefaultChunkCharacterLimit = 120_000;
    public const int DefaultMaxChunks = 20;
    private const int PromptVersion = 3;
    private static readonly TimeSpan InvocationTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan TotalTimeout = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, DiffAnalysisResultModel> _cache = new(StringComparer.Ordinal);
    private readonly int _chunkCharacterLimit;
    private readonly ICodexStructuredRunnerPort _client;
    private readonly ICodexSessionClientPort _session;
    private readonly IDiffAnalysisProviderPort _fallback;
    private readonly int _maxChunks;

    public CodexCliDiffAnalysisAdapter(
        ICodexStructuredRunnerPort client,
        ICodexSessionClientPort session,
        IDiffAnalysisProviderPort fallback,
        int chunkCharacterLimit = DefaultChunkCharacterLimit,
        int maxChunks = DefaultMaxChunks)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkCharacterLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxChunks);
        _client = client;
        _session = session;
        _fallback = fallback;
        _chunkCharacterLimit = chunkCharacterLimit;
        _maxChunks = maxChunks;
    }

    public async Task<DiffAnalysisResultModel> AnalyzeAsync(DiffAnalysisRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var assessment = AiDiffSafetyPolicy.Assess(request.Diff, request.Scope);
        var hunks = assessment.TransferableHunks.Concat(assessment.ExcludedHunks).ToList();
        if (hunks.Count == 0) return new DiffAnalysisResultModel(hunks, [], DiffAnalysisSourceModel.Codex);

        var excludedHunks = assessment.ExcludedHunks;
        if (excludedHunks.Count > 0)
        {
            var allowedHunks = assessment.TransferableHunks;
            var local = await _fallback.AnalyzeAsync(request with { Diff = BuildDiff(excludedHunks) },
                cancellationToken);
            if (allowedHunks.Count == 0)
                return local with
                {
                    Hunks = hunks,
                    Diagnostic = $"{excludedHunks.Count} excluded file change(s) were analysed locally.",
                    AiPreview = assessment.Preview
                };

            var remote = await AnalyzeAsync(request with { Diff = BuildDiff(allowedHunks) }, cancellationToken);
            return new DiffAnalysisResultModel(hunks, remote.Groups.Concat(local.Groups).ToList(), remote.Source,
                $"{excludedHunks.Count} excluded file change(s) were analysed locally.", assessment.Preview);
        }

        var cacheKey = CacheKey(request);
        if (_cache.TryGetValue(cacheKey, out var cached)) return cached;

        using var totalTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        totalTimeout.CancelAfter(TotalTimeout);
        try
        {
            var status = await _session.GetStatusAsync(totalTimeout.Token);
            if (!status.IsReady)
                return await FallbackAsync(request, StatusDiagnostic(status.Kind, request.Language),
                    totalTimeout.Token);

            var inputs = hunks.Select((hunk, index) => new HunkInput($"H{index + 1:D4}", hunk)).ToList();
            if (inputs.Any(input => JsonSerializer.Serialize(input.Payload, JsonOptions).Length > _chunkCharacterLimit))
                return await FallbackAsync(request, Localize(request.Language,
                        "A changed section is too large for GPT-6 Luna; the rule-based grouping is shown.",
                        "Ein geänderter Abschnitt ist für GPT-6 Luna zu groß; die regelbasierte Gruppierung wird angezeigt."),
                    totalTimeout.Token);
            var chunks = Chunk(inputs);
            if (chunks.Count > _maxChunks)
                return await FallbackAsync(request, Localize(request.Language,
                        "The diff is too large for GPT-6 Luna; the rule-based grouping is shown.",
                        "Der Diff ist für GPT-6 Luna zu groß; die regelbasierte Gruppierung wird angezeigt."),
                    totalTimeout.Token);

            var provisional = new List<ProvisionalGroup>();
            foreach (var chunk in chunks)
            {
                var response = await _client.RunStructuredAsync(
                    BuildChunkPrompt(chunk, request.Language),
                    BuildAnalysisSchema(chunk.Select(x => x.Id)),
                    InvocationTimeout,
                    totalTimeout.Token);
                foreach (var group in ParseAnalysis(response, chunk.Select(x => x.Id)))
                    provisional.Add(new ProvisionalGroup($"G{provisional.Count + 1:D4}", group.Title, group.Description,
                        group.ItemIds, group.Confidence, group.Review));
            }

            IReadOnlyList<ParsedGroup> finalGroups;
            if (provisional.Count == 0) throw new InvalidDataException("GPT-6 Luna returned no groups.");
            if (chunks.Count == 1)
            {
                finalGroups = provisional.Select(x => new ParsedGroup(x.Title, x.Description, x.HunkIds, x.Confidence,
                        x.Review))
                    .ToList();
            }
            else
            {
                var response = await _client.RunStructuredAsync(
                    BuildMergePrompt(provisional, inputs, request.Language),
                    BuildMergeSchema(provisional.Select(x => x.Id)),
                    InvocationTimeout,
                    totalTimeout.Token);
                var merged = ParseMerge(response, provisional.Select(x => x.Id));
                var byGroupId = provisional.ToDictionary(x => x.Id, StringComparer.Ordinal);
                finalGroups = merged.Select(group => new ParsedGroup(
                    group.Title,
                    group.Description,
                    group.ItemIds.SelectMany(id => byGroupId[id].HunkIds).ToList(),
                    group.Confidence,
                    group.Review)).ToList();
            }

            ValidateExactAssignment(finalGroups, inputs.Select(x => x.Id));
            var hunkById = inputs.ToDictionary(x => x.Id, x => x.Hunk, StringComparer.Ordinal);
            var order = inputs.Select((x, index) => (x.Id, index))
                .ToDictionary(x => x.Id, x => x.index, StringComparer.Ordinal);
            var groups = finalGroups.Select(group => new WorkStepGroupModel(
                group.Title,
                group.Description,
                group.ItemIds.OrderBy(id => order[id]).Select(id => hunkById[id]).ToList(),
                group.Confidence,
                group.Review)).ToList();
            var result = new DiffAnalysisResultModel(hunks, groups, DiffAnalysisSourceModel.Codex, AiPreview: assessment.Preview);
            _cache[cacheKey] = result;
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return await FallbackAsync(request, Localize(request.Language,
                    "GPT-6 Luna analysis exceeded the ten-minute limit; the rule-based grouping is shown.",
                    "Die GPT-6-Luna-Analyse hat das Zehn-Minuten-Limit überschritten; die regelbasierte Gruppierung wird angezeigt."),
                CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or TimeoutException
                                       or CodexCliInvocationException or Win32Exception)
        {
            return await FallbackAsync(request, FailureDiagnostic(ex, request.Language), CancellationToken.None);
        }
    }

    #region Private

    private async Task<DiffAnalysisResultModel> FallbackAsync(DiffAnalysisRequestModel request, string diagnostic,
        CancellationToken cancellationToken)
    {
        var result = await _fallback.AnalyzeAsync(request, cancellationToken);
        return result with { Source = DiffAnalysisSourceModel.Heuristic, Diagnostic = diagnostic };
    }

    private List<List<HunkInput>> Chunk(IReadOnlyList<HunkInput> inputs)
    {
        var chunks = new List<List<HunkInput>>();
        var current = new List<HunkInput>();
        var currentLength = 0;
        foreach (var input in inputs)
        {
            var length = JsonSerializer.Serialize(input.Payload, JsonOptions).Length;
            if (current.Count > 0 && currentLength + length > _chunkCharacterLimit)
            {
                chunks.Add(current);
                current = [];
                currentLength = 0;
            }

            current.Add(input);
            currentLength += length;
        }

        if (current.Count > 0) chunks.Add(current);
        return chunks;
    }

    private static string CacheKey(DiffAnalysisRequestModel request)
    {
        var value =
            $"{PromptVersion}\n{request.RepositoryPath}\n{request.Scope}\n{request.Language}\n{request.Diff.Replace("\r\n", "\n", StringComparison.Ordinal)}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string BuildDiff(IEnumerable<DiffHunkModel> hunks)
    {
        return string.Join("\n",
            hunks.Select(hunk => $"diff --git a/{hunk.FilePath} b/{hunk.FilePath}\n{hunk.Header}\n{hunk.Content}"));
    }

    private static string StatusDiagnostic(CodexCliStatusKindModel status, string language)
    {
        return status switch
        {
            CodexCliStatusKindModel.Missing => Localize(language,
                "The Codex CLI is not installed; the rule-based grouping is shown.",
                "Die Codex CLI ist nicht installiert; die regelbasierte Gruppierung wird angezeigt."),
            CodexCliStatusKindModel.NotAuthenticated => Localize(language,
                "Codex is not signed in with ChatGPT; the rule-based grouping is shown.",
                "Codex ist nicht mit ChatGPT angemeldet; die regelbasierte Gruppierung wird angezeigt."),
            CodexCliStatusKindModel.ApiKeyAuthentication or CodexCliStatusKindModel.UnsupportedAuthentication => Localize(
                language, "Codex is not using a ChatGPT sign-in; the rule-based grouping is shown.",
                "Codex verwendet keine ChatGPT-Anmeldung; die regelbasierte Gruppierung wird angezeigt."),
            _ => Localize(language, "Codex is unavailable; the rule-based grouping is shown.",
                "Codex ist nicht verfügbar; die regelbasierte Gruppierung wird angezeigt.")
        };
    }

    private static string FailureDiagnostic(Exception exception, string language)
    {
        var unavailableModel = exception is CodexCliInvocationException invocation &&
                               invocation.Diagnostic.Contains("model", StringComparison.OrdinalIgnoreCase) &&
                               (invocation.Diagnostic.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
                                invocation.Diagnostic.Contains("not available", StringComparison.OrdinalIgnoreCase) ||
                                invocation.Diagnostic.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                                invocation.Diagnostic.Contains("unsupported", StringComparison.OrdinalIgnoreCase));
        if (unavailableModel)
            return Localize(language,
                "GPT-6 Luna is not available in this workspace; the rule-based grouping is shown.",
                "GPT-6 Luna ist in diesem Workspace nicht verfügbar; die regelbasierte Gruppierung wird angezeigt.");
        if (exception is TimeoutException)
            return Localize(language, "GPT-6 Luna analysis timed out; the rule-based grouping is shown.",
                "Die GPT-6-Luna-Analyse hat das Zeitlimit überschritten; die regelbasierte Gruppierung wird angezeigt.");
        if (exception is InvalidDataException data)
            return Localize(language,
                $"GPT-6 Luna returned an unusable assignment ({data.Message}); the rule-based grouping is shown.",
                $"Die GPT-6-Luna-Zuordnung war nicht verwendbar ({data.Message}); die regelbasierte Gruppierung wird angezeigt.");
        if (exception is JsonException)
            return Localize(language, "GPT-6 Luna returned invalid JSON; the rule-based grouping is shown.",
                "GPT-6 Luna hat ungültiges JSON geliefert; die regelbasierte Gruppierung wird angezeigt.");
        if (exception is CodexCliInvocationException failed)
        {
            var detail = LastDiagnosticLine(failed.Diagnostic);
            return Localize(language,
                $"The Codex CLI failed{detail}; the rule-based grouping is shown.",
                $"Der Codex-CLI-Aufruf ist fehlgeschlagen{detail}; die regelbasierte Gruppierung wird angezeigt.");
        }

        return Localize(language, "GPT-6 Luna analysis failed; the rule-based grouping is shown.",
            "Die GPT-6-Luna-Analyse ist fehlgeschlagen; die regelbasierte Gruppierung wird angezeigt.");
    }

    private static string LastDiagnosticLine(string diagnostic)
    {
        var line = diagnostic
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault();
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('{')) return string.Empty;
        return $": {(line.Length <= 240 ? line : line[..240])}";
    }

    private static string Localize(string language, string english, string german)
    {
        return language.StartsWith("de", StringComparison.OrdinalIgnoreCase) ? german : english;
    }

    #endregion
}
