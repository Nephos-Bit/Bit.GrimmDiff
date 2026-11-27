namespace GrimmDiff.Logic.Abstracts.Models;

public sealed record WorkStepGroupModel(
    string Title,
    string Description,
    IReadOnlyList<DiffHunkModel> Hunks,
    double Confidence,
    DiffGroupReviewModel? Review = null);