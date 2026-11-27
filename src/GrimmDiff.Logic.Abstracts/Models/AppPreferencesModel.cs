namespace GrimmDiff.Logic.Abstracts.Models;

public sealed record AppPreferencesModel(
    IReadOnlyList<string> RecentRepositories,
    string Theme = "System",
    string Language = "English",
    string Font = "Cascadia Code",
    IReadOnlyList<AiRepositoryApprovalModel>? AiRepositoryApprovals = null,
    bool AiRiskNoticeAccepted = false,
    string? LastShownReleaseNotesVersion = null,
    TicketLinkSettingsModel? TicketLinks = null,
    bool MascotsEnabled = true,
    string MascotId = "moss")
{
    public IReadOnlyList<AiRepositoryApprovalModel> Approvals => AiRepositoryApprovals ?? [];
    public TicketLinkSettingsModel TicketLinkConfiguration => TicketLinks ?? TicketLinkSettingsModel.Disabled;
    public static AppPreferencesModel Empty { get; } = new([]);
}
