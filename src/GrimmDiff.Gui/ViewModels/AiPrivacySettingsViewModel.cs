namespace GrimmDiff.Gui;

public sealed class AiPrivacySettingsViewModel(MainViewModel main) :
    SettingsPageViewModelBase(main, "ai-privacy", "KI", "Datenschutz & Freigaben", 1)
{
    public AsyncCommand ShowRiskNoticeCommand => Main.ShowAiRiskNoticeCommand;
    public AsyncCommand RevokeApprovalCommand => Main.RevokeAiApprovalCommand;
    public bool HasRepository => Main.HasRepository;
}
