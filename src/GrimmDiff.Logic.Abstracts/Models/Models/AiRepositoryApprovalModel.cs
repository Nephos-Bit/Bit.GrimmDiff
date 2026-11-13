namespace GrimmDiff.Logic.Abstracts;

public sealed record AiRepositoryApprovalModel(
    string RepositoryId,
    string ProviderId,
    string SafetyPolicyVersion,
    DateTimeOffset ApprovedAt);