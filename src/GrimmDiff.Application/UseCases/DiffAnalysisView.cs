using GrimmDiff.Domain;
namespace GrimmDiff.Application;
public sealed record DiffAnalysisView(DiffAnalysisResult Result, bool IsAiRequested);
