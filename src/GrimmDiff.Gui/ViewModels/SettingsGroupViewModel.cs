using System.ComponentModel;

namespace GrimmDiff.Gui;

public sealed class SettingsGroupViewModel : INotifyPropertyChanged
{
    public SettingsGroupViewModel(IReadOnlyList<ISettingsPageViewModel> pages)
    {
        Pages = pages;
        foreach (var page in pages.OfType<INotifyPropertyChanged>())
            page.PropertyChanged += (_, _) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
    }

    public string Title => Pages[0].Group;
    public IReadOnlyList<ISettingsPageViewModel> Pages { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
}
