using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GrimmDiff.Gui;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private ISettingsPageViewModel _selectedPage;

    public SettingsViewModel()
    {
        var pages = new ISettingsPageViewModel[]
        {
            new SettingsPageViewModel("appearance", "Allgemein", "Darstellung", 0),
            new SettingsPageViewModel("ai-account", "KI", "Konto", 0),
            new SettingsPageViewModel("ai-privacy", "KI", "Datenschutz & Freigaben", 1),
            new SettingsPageViewModel("ticket-links", "Integrationen", "Ticket-Verlinkung", 0),
            new SettingsPageViewModel("about", "Über", "Updates & Info", 0)
        };
        Groups = pages.GroupBy(page => page.Group)
            .Select(group => new SettingsGroupViewModel(group.Key, group.OrderBy(page => page.Order).ToList()))
            .ToList();
        _selectedPage = pages[0];
        _selectedPage.IsSelected = true;
        SelectPageCommand = new DelegateCommand<ISettingsPageViewModel>(page => SelectedPage = page);
    }

    public IReadOnlyList<SettingsGroupViewModel> Groups { get; }
    public DelegateCommand<ISettingsPageViewModel> SelectPageCommand { get; }

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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAppearanceSelected)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAiAccountSelected)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAiPrivacySelected)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTicketLinksSelected)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAboutSelected)));
        }
    }

    public bool IsAppearanceSelected => SelectedPage.Id == "appearance";
    public bool IsAiAccountSelected => SelectedPage.Id == "ai-account";
    public bool IsAiPrivacySelected => SelectedPage.Id == "ai-privacy";
    public bool IsTicketLinksSelected => SelectedPage.Id == "ticket-links";
    public bool IsAboutSelected => SelectedPage.Id == "about";

    public void OpenDefault() => SelectedPage = Groups.SelectMany(group => group.Pages).First();

    public event PropertyChangedEventHandler? PropertyChanged;
}
