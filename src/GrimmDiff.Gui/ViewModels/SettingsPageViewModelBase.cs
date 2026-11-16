using System.ComponentModel;

namespace GrimmDiff.Gui;

public abstract class SettingsPageViewModelBase : ISettingsPageViewModel, INotifyPropertyChanged
{
    private bool _isSelected;
    private readonly string _groupKey;
    private readonly string _titleKey;

    protected SettingsPageViewModelBase(MainViewModel main, string id, string groupKey, string titleKey, int order)
    {
        Main = main;
        Id = id;
        _groupKey = groupKey;
        _titleKey = titleKey;
        Order = order;
        Main.PropertyChanged += (_, _) => RefreshBindings();
    }

    protected MainViewModel Main { get; }
    public LocalizedTextService Text => Main.Text;
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

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected void RefreshBindings()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
