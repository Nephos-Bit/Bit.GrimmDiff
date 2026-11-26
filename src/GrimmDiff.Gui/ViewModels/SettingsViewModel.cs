using System.ComponentModel;

namespace GrimmDiff.Gui;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private ISettingsPageViewModel _selectedPage;

    public SettingsViewModel(MainViewModel main)
    {
        var pages = new ISettingsPageViewModel[]
        {
            new AppearanceSettingsViewModel(main),
            new MascotSettingsViewModel(main),
            new AiAccountSettingsViewModel(main),
            new AiPrivacySettingsViewModel(main),
            new AiPromptSettingsViewModel(main),
            new TicketLinksSettingsViewModel(main),
            new AboutSettingsViewModel(main)
        };
        Groups = pages.GroupBy(page => page.Group)
            .Select(group => new SettingsGroupViewModel(group.OrderBy(page => page.Order).ToList()))
            .ToList();
        _selectedPage = pages[0];
        _selectedPage.IsSelected = true;
        SelectPageCommand = new DelegateCommand<ISettingsPageViewModel>(page => SelectedPage = page);
        BackToWorkspaceCommand = main.BackToWorkspaceCommand;
    }

    public IReadOnlyList<SettingsGroupViewModel> Groups { get; }
    public DelegateCommand<ISettingsPageViewModel> SelectPageCommand { get; }
    public DelegateCommand<object> BackToWorkspaceCommand { get; }

    public ISettingsPageViewModel SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (ReferenceEquals(_selectedPage, value)) return;
            _selectedPage.IsSelected = false;
            _selectedPage = value;
            _selectedPage.IsSelected = true;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedPage)));
        }
    }

    public void OpenDefault() => SelectedPage = Groups.SelectMany(group => group.Pages).First();

    public event PropertyChangedEventHandler? PropertyChanged;
}
