namespace GrimmDiff.Logic.Abstracts.Models;

public sealed record DiffAnalysisResultModel(
    IReadOnlyList<DiffHunkModel> Hunks,
    IReadOnlyList<WorkStepGroupModel> Groups,
    DiffAnalysisSourceModel Source,
    string? Diagnostic = null,
    AiAnalysisPreviewModel? AiPreview = null);