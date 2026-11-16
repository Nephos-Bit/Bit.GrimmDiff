namespace GrimmDiff.Gui;

public sealed class AiAccountSettingsViewModel(MainViewModel main) :
    SettingsPageViewModelBase(main, "ai-account", "Ai", "Account", 0)
{
    public string StatusText => Main.CodexStatusText;
    public string AccountText => Main.CodexAccountText;
    public string WeeklyQuotaText => Main.WeeklyQuotaText;
    public string WeeklyQuotaToolTip => Main.WeeklyQuotaToolTip;
    public AsyncCommand LoginCommand => Main.LoginToCodexCommand;
}
