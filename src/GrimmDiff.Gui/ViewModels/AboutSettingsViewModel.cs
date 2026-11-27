using GrimmDiff.Gui.Services;
using GrimmDiff.Gui.Commands;
using GrimmDiff.Gui.Abstracts.Services;
namespace GrimmDiff.Gui.ViewModels;

public sealed class AboutSettingsViewModel : SettingsPageViewModelBase
{
    private readonly ApplicationUpdateCoordinator _updates;
    public AboutSettingsViewModel(PreferencesStateService preferences, ApplicationUpdateCoordinator updates,
        IExternalNavigationService navigation) : base(preferences, "about", "About", "UpdatesAndInfo", 0)
    {
        _updates = updates;
        CheckForUpdatesCommand = new AsyncCommand(_updates.CheckManuallyAsync);
        OpenUrlCommand = new DelegateCommand<string>(navigation.Open);
    }
    public string ApplicationVersion => _updates.ApplicationVersion;
    public AsyncCommand CheckForUpdatesCommand { get; }
    public DelegateCommand<string> OpenUrlCommand { get; }
}
