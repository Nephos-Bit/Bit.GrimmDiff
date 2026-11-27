namespace GrimmDiff.Logic.Abstracts.Models;

public sealed record AiDiffSafetyAssessmentModel(
    IReadOnlyList<DiffHunkModel> TransferableHunks,
    IReadOnlyList<DiffHunkModel> ExcludedHunks,
    AiAnalysisPreviewModel Preview);