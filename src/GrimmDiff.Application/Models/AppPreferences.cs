namespace GrimmDiff.Application;

public sealed record AppPreferences(
    IReadOnlyList<string> RecentRepositories,
    string Theme = "System",
    string Language = "English",
    string Font = "Cascadia Code",
    IReadOnlyList<AiRepositoryApproval>? AiRepositoryApprovals = null,
    bool AiRiskNoticeAccepted = false,
    string? LastShownReleaseNotesVersion = null)
{
    public IReadOnlyList<AiRepositoryApproval> Approvals => AiRepositoryApprovals ?? [];
    public static AppPreferences Empty { get; } = new([]);
}