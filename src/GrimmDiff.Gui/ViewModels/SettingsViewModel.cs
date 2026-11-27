using GrimmDiff.Gui.Commands;
using GrimmDiff.Gui.Services;

namespace GrimmDiff.Gui.ViewModels;

public sealed class SettingsViewModel : ObservableViewModel
{
    private ISettingsPageViewModel _selectedPage;

    public SettingsViewModel(PreferencesStateService preferences,
        AppearanceSettingsViewModel appearance, MascotSettingsViewModel mascot,
        AiAccountSettingsViewModel account, AiPrivacySettingsViewModel privacy,
        AiPromptSettingsViewModel prompt, TicketLinksSettingsViewModel tickets,
        AboutSettingsViewModel about)
    {
        var pages = new ISettingsPageViewModel[]
        {
            appearance, mascot, account, privacy, prompt, tickets, about
        };
        Groups = pages.GroupBy(page => page.Group)
            .Select(group => new SettingsGroupViewModel(group.OrderBy(page => page.Order).ToList()))
            .ToList();
        _selectedPage = pages[0];
        _selectedPage.IsSelected = true;
        SelectPageCommand = new DelegateCommand<ISettingsPageViewModel>(page => SelectedPage = page);
        BackToWorkspaceCommand = new DelegateCommand<object>(_ => BackRequested?.Invoke());
        Text = preferences.Text;
        preferences.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(PreferencesStateService.Language) or "")
                OnPropertyChanged(nameof(Text));
        };
    }

    public IReadOnlyList<SettingsGroupViewModel> Groups { get; }
    public DelegateCommand<ISettingsPageViewModel> SelectPageCommand { get; }
    public DelegateCommand<object> BackToWorkspaceCommand { get; }
    public LocalizedTextService Text { get; }
    public event Action? BackRequested;

    public ISettingsPageViewModel SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (ReferenceEquals(_selectedPage, value)) return;
            _selectedPage.IsSelected = false;
            _selectedPage = value;
            _selectedPage.IsSelected = true;
            OnPropertyChanged(nameof(SelectedPage));
        }
    }

    public void OpenDefault() => SelectedPage = Groups.SelectMany(group => group.Pages).First();

}
