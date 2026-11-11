namespace GrimmDiff.Application;

public sealed record AiRepositoryApproval(string RepositoryId, string CodexPath, string CodexVersion, DateTimeOffset ApprovedAt);
