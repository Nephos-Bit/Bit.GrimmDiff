using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Logic.Abstracts;

public interface IDiffAnalysisProviderPort
{
    Task<DiffAnalysisResultModel> AnalyzeAsync(DiffAnalysisRequestModel request, CancellationToken cancellationToken = default);
}