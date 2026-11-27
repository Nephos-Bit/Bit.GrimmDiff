using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.Abstracts.Ports;

namespace GrimmDiff.Logic.Tests;

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