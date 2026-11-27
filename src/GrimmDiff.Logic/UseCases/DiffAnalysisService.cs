using GrimmDiff.Logic.Abstracts.Policies;
using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.Abstracts.Ports;

namespace GrimmDiff.Logic.UseCases;

public sealed class DiffAnalysisService(
    IRepositoryDiffReaderPort diffs,
    IDiffAnalysisProviderPort aiProvider,
    IDiffAnalysisProviderPort heuristicProvider)
{
    public async Task<DiffAnalysisResultModel> AnalyzeAsync(DiffAnalysisQueryModel query,
        CancellationToken cancellationToken = default)
    {
        var (diff, scope) = await ReadDiffAsync(query, cancellationToken);
        var request = new DiffAnalysisRequestModel(query.RepositoryPath, scope, diff, query.Language);
        return await (query.UseAi ? aiProvider : heuristicProvider).AnalyzeAsync(request, cancellationToken);
    }

    public async Task<AiAnalysisPreviewModel> PreviewAiAsync(DiffAnalysisQueryModel query,
        CancellationToken cancellationToken = default)
    {
        var (diff, scope) = await ReadDiffAsync(query, cancellationToken);
        return AiDiffSafetyPolicy.Assess(diff, scope).Preview;
    }

    private async Task<(string Diff, DiffScopeModel Scope)> ReadDiffAsync(DiffAnalysisQueryModel query,
        CancellationToken cancellationToken)
    {
        var historical = !string.IsNullOrWhiteSpace(query.CommitId);
        var diff = historical
            ? await diffs.GetCommitDiffAsync(query.RepositoryPath, query.CommitId!,
                cancellationToken: cancellationToken)
            : await diffs.GetDiffAsync(query.RepositoryPath, DiffScopeModel.Staged, cancellationToken: cancellationToken) +
              await diffs.GetDiffAsync(query.RepositoryPath, DiffScopeModel.WorkingTree,
                  cancellationToken: cancellationToken);
        return (diff, historical ? DiffScopeModel.Historical : DiffScopeModel.WorkingTree);
    }
}
