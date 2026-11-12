namespace GrimmDiff.Domain;

public sealed record AiDiffSafetyAssessment(
    IReadOnlyList<DiffHunk> TransferableHunks,
    IReadOnlyList<DiffHunk> ExcludedHunks,
    AiAnalysisPreview Preview);