namespace GrimmDiff.Gui;

public sealed class AboutSettingsViewModel(MainViewModel main) :
    SettingsPageViewModelBase(main, "about", "About", "UpdatesAndInfo", 0)
{
    public string ApplicationVersion => Main.ApplicationVersion;
    public AsyncCommand CheckForUpdatesCommand => Main.CheckForUpdatesCommand;
    public DelegateCommand<string> OpenUrlCommand => Main.OpenUrlCommand;
}
