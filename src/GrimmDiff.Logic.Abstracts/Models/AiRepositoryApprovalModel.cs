namespace GrimmDiff.Logic.Abstracts.Models;

public sealed record AiRepositoryApprovalModel(
    string RepositoryId,
    string ProviderId,
    string SafetyPolicyVersion,
    DateTimeOffset ApprovedAt);