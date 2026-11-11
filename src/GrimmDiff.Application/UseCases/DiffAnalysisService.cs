using GrimmDiff.Domain;

namespace GrimmDiff.Application;

public sealed class DiffAnalysisService(
    IRepositoryDiffReader diffs,
    IDiffAnalysisProvider aiProvider,
    IDiffAnalysisProvider heuristicProvider)
{
    public async Task<DiffAnalysisView> AnalyzeAsync(DiffAnalysisQuery query,
        CancellationToken cancellationToken = default)
    {
        var (diff, scope) = await ReadDiffAsync(query, cancellationToken);
        var request = new DiffAnalysisRequest(query.RepositoryPath, scope, diff, query.Language);
        return new DiffAnalysisView(
            await (query.UseAi ? aiProvider : heuristicProvider).AnalyzeAsync(request, cancellationToken), query.UseAi);
    }

    public async Task<AiAnalysisPreview> PreviewAiAsync(DiffAnalysisQuery query,
        CancellationToken cancellationToken = default)
    {
        var (diff, scope) = await ReadDiffAsync(query, cancellationToken);
        return AiDiffSafetyPolicy.Assess(diff, scope).Preview;
    }

    private async Task<(string Diff, DiffScope Scope)> ReadDiffAsync(DiffAnalysisQuery query,
        CancellationToken cancellationToken)
    {
        var historical = !string.IsNullOrWhiteSpace(query.CommitId);
        var diff = historical
            ? await diffs.GetCommitDiffAsync(query.RepositoryPath, query.CommitId!,
                cancellationToken: cancellationToken)
            : await diffs.GetDiffAsync(query.RepositoryPath, DiffScope.Staged, cancellationToken: cancellationToken) +
              await diffs.GetDiffAsync(query.RepositoryPath, DiffScope.WorkingTree,
                  cancellationToken: cancellationToken);
        return (diff, historical ? DiffScope.Historical : DiffScope.WorkingTree);
    }
}