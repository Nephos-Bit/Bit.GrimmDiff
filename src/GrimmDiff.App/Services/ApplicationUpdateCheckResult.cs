namespace GrimmDiff.App;

public sealed record ApplicationUpdateCheckResult(ApplicationUpdateStatus Status, string? Version = null);
