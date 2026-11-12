namespace GrimmDiff.Domain;

public sealed record WorkStepGroup(string Title, string Description, IReadOnlyList<DiffHunk> Hunks,
    double Confidence, DiffGroupReview? Review = null);
