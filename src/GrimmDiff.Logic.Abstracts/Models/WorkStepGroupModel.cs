namespace GrimmDiff.Logic.Abstracts;

public sealed record WorkStepGroupModel(
    string Title,
    string Description,
    IReadOnlyList<DiffHunkModel> Hunks,
    double Confidence,
    DiffGroupReviewModel? Review = null);