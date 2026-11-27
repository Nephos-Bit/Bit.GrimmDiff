using GrimmDiff.Gui.Services;
namespace GrimmDiff.Gui.ViewModels;

public abstract class SettingsPageViewModelBase : ObservableViewModel, ISettingsPageViewModel
{
    private bool _isSelected;
    private readonly string _groupKey;
    private readonly string _titleKey;

    protected SettingsPageViewModelBase(PreferencesStateService preferences, string id, string groupKey, string titleKey, int order)
    {
        Preferences = preferences;
        Id = id;
        _groupKey = groupKey;
        _titleKey = titleKey;
        Order = order;
        Preferences.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(PreferencesStateService.Language) or "")
            {
                OnPropertyChanged(nameof(Group));
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Text));
            }
        };
    }

    protected PreferencesStateService Preferences { get; }
    public LocalizedTextService Text => Preferences.Text;
    public string Id { get; }
    public string Group => Text[_groupKey];
    public string Title => Text[_titleKey];
    public int Order { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

}
