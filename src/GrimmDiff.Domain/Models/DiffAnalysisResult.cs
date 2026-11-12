namespace GrimmDiff.Domain;

public sealed record DiffAnalysisResult(
    IReadOnlyList<DiffHunk> Hunks,
    IReadOnlyList<WorkStepGroup> Groups,
    DiffAnalysisSource Source,
    string? Diagnostic = null,
    AiAnalysisPreview? AiPreview = null);