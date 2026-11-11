using GrimmDiff.Domain;

namespace GrimmDiff.Application;

public interface IDiffAnalysisProvider
{
    Task<DiffAnalysisResult> AnalyzeAsync(DiffAnalysisRequest request, CancellationToken cancellationToken = default);
}