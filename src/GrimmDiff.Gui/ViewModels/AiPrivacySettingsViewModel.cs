namespace GrimmDiff.Gui;

public sealed class AiPrivacySettingsViewModel(MainViewModel main) :
    SettingsPageViewModelBase(main, "ai-privacy", "Ai", "PrivacyAndApprovals", 1)
{
    public AsyncCommand ShowRiskNoticeCommand => Main.ShowAiRiskNoticeCommand;
    public AsyncCommand RevokeApprovalCommand => Main.RevokeAiApprovalCommand;
    public bool HasRepository => Main.HasRepository;
}
