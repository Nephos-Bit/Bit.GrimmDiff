namespace GrimmDiff.Logic.Abstracts;

public sealed record AiDiffSafetyAssessmentModel(
    IReadOnlyList<DiffHunkModel> TransferableHunks,
    IReadOnlyList<DiffHunkModel> ExcludedHunks,
    AiAnalysisPreviewModel Preview);