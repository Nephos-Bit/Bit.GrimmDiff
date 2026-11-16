using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GrimmDiff.Gui;

public sealed class SettingsPageViewModel(string id, string group, string title, int order) : ISettingsPageViewModel,
    INotifyPropertyChanged
{
    private bool _isSelected;

    public string Id { get; } = id;
    public string Group { get; } = group;
    public string Title { get; } = title;
    public int Order { get; } = order;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
