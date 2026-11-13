namespace GrimmDiff.Logic.Abstracts;

public sealed record AppPreferencesModel(
    IReadOnlyList<string> RecentRepositories,
    string Theme = "System",
    string Language = "English",
    string Font = "Cascadia Code",
    IReadOnlyList<AiRepositoryApprovalModel>? AiRepositoryApprovals = null,
    bool AiRiskNoticeAccepted = false,
    string? LastShownReleaseNotesVersion = null)
{
    public IReadOnlyList<AiRepositoryApprovalModel> Approvals => AiRepositoryApprovals ?? [];
    public static AppPreferencesModel Empty { get; } = new([]);
}