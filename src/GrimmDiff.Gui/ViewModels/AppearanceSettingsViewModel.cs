using GrimmDiff.Gui.Services;
namespace GrimmDiff.Gui.ViewModels;

public sealed class AppearanceSettingsViewModel :
    SettingsPageViewModelBase
{
    public AppearanceSettingsViewModel(PreferencesStateService preferences) :
        base(preferences, "appearance", "General", "Appearance", 0)
    {
        preferences.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(PreferencesStateService.Language) or "") OnPropertyChanged(nameof(SelectedLanguage));
            if (args.PropertyName is nameof(PreferencesStateService.Theme) or "") OnPropertyChanged(nameof(SelectedTheme));
            if (args.PropertyName is nameof(PreferencesStateService.Font) or "") OnPropertyChanged(nameof(SelectedFont));
        };
    }
    public IReadOnlyList<string> Languages { get; } = ["English", "Deutsch"];
    public IReadOnlyList<string> Themes { get; } = ["System", "Dark", "Light"];
    public IReadOnlyList<string> Fonts { get; } = ["Cascadia Code", "Inter", "Verdana"];

    public string SelectedLanguage
    {
        get => Preferences.Language;
        set => Preferences.SetLanguage(value);
    }

    public string SelectedTheme
    {
        get => Preferences.Theme;
        set => Preferences.SetTheme(value);
    }

    public string SelectedFont
    {
        get => Preferences.Font;
        set => Preferences.SetFont(value);
    }
}
