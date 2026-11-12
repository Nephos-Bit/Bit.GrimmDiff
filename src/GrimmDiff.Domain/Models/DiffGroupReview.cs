namespace GrimmDiff.Domain;

public sealed record DiffGroupReview(DiffGroupReviewStatus Status, string? Hint);