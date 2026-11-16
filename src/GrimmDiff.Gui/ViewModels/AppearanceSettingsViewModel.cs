namespace GrimmDiff.Gui;

public sealed class AppearanceSettingsViewModel(MainViewModel main) :
    SettingsPageViewModelBase(main, "appearance", "General", "Appearance", 0)
{
    public IReadOnlyList<string> Languages => Main.Languages;
    public IReadOnlyList<string> Themes => Main.Themes;
    public IReadOnlyList<string> Fonts => Main.Fonts;

    public string SelectedLanguage
    {
        get => Main.SelectedLanguage;
        set => Main.SelectedLanguage = value;
    }

    public string SelectedTheme
    {
        get => Main.SelectedTheme;
        set => Main.SelectedTheme = value;
    }

    public string SelectedFont
    {
        get => Main.SelectedFont;
        set => Main.SelectedFont = value;
    }
}
