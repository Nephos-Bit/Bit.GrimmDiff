namespace GrimmDiff.Application;

public sealed record AiRepositoryApproval(
    string RepositoryId,
    string ProviderId,
    string SafetyPolicyVersion,
    DateTimeOffset ApprovedAt);
