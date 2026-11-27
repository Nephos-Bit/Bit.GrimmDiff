using System.Windows;
using System.Windows.Media;
using GrimmDiff.Gui.Commands;
using GrimmDiff.Gui.Services;

namespace GrimmDiff.Gui.ViewModels;

public sealed class MainViewModel : ObservableViewModel
{
    private readonly PreferencesStateService _preferences;
    private readonly AiConsentService _consent;
    private readonly ApplicationUpdateCoordinator _updates;
    private int _initialized;
    private bool _settingsOpen;

    public MainViewModel(WorkspaceViewModel workspace, CodexAccountViewModel account,
        SettingsViewModel settings, PreferencesStateService preferences, AiConsentService consent,
        ApplicationUpdateCoordinator updates)
    {
        Workspace = workspace;
        Account = account;
        Settings = settings;
        _preferences = preferences;
        _consent = consent;
        _updates = updates;
        Settings.BackRequested += () => IsSettingsOpen = false;
        OpenSettingsCommand = new DelegateCommand<object>(_ =>
        {
            if (IsSettingsOpen)
            {
                IsSettingsOpen = false;
                return;
            }
            Settings.OpenDefault();
            IsSettingsOpen = true;
        });
        preferences.PropertyChanged += OnPreferencesChanged;
    }

    public WorkspaceViewModel Workspace { get; }
    public CodexAccountViewModel Account { get; }
    public SettingsViewModel Settings { get; }
    public string ApplicationVersion => _updates.ApplicationVersion;
    public DelegateCommand<object> OpenSettingsCommand { get; }
    public bool IsSettingsOpen
    {
        get => _settingsOpen;
        private set
        {
            if (Set(ref _settingsOpen, value)) OnPropertyChanged(nameof(IsWorkspaceOpen));
        }
    }
    public bool IsWorkspaceOpen => !IsSettingsOpen;

    public async Task InitializeAsync()
    {
        if (Interlocked.Exchange(ref _initialized, 1) != 0) return;
        await _preferences.LoadAsync();
        ThemeService.Apply(_preferences.Theme);
        Application.Current.Resources["AppFontFamily"] = new FontFamily(_preferences.Font);
        if (!_preferences.AiRiskNoticeAccepted && !_consent.ShowRiskNotice())
        {
            Application.Current.Shutdown();
            return;
        }
        await _preferences.FlushAsync();
        await _updates.ShowReleaseNotesIfNeededAsync();
        await Task.Delay(750);
        await Account.CheckAsync();
    }

    public Task CheckForUpdatesOnStartupAsync() => _updates.CheckOnStartupAsync();

    private void OnPreferencesChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PreferencesStateService.Theme)) ThemeService.Apply(_preferences.Theme);
        if (e.PropertyName == nameof(PreferencesStateService.Font))
            Application.Current.Resources["AppFontFamily"] = new FontFamily(_preferences.Font);
    }
}
