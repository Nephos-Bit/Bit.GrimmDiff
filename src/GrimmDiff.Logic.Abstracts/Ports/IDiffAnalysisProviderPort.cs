using GrimmDiff.Logic.Abstracts.Models;

namespace GrimmDiff.Logic.Abstracts.Ports;

public interface IDiffAnalysisProviderPort
{
    Task<DiffAnalysisResultModel> AnalyzeAsync(DiffAnalysisRequestModel request, CancellationToken cancellationToken = default);
}