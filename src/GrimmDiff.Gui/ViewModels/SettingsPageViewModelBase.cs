using System.ComponentModel;

namespace GrimmDiff.Gui;

public abstract class SettingsPageViewModelBase : ISettingsPageViewModel, INotifyPropertyChanged
{
    private bool _isSelected;

    protected SettingsPageViewModelBase(MainViewModel main, string id, string group, string title, int order)
    {
        Main = main;
        Id = id;
        Group = group;
        Title = title;
        Order = order;
        Main.PropertyChanged += (_, _) => RefreshBindings();
    }

    protected MainViewModel Main { get; }
    public string Id { get; }
    public string Group { get; }
    public string Title { get; }
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
