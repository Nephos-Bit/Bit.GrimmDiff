using System.ComponentModel;

namespace GrimmDiff.Gui.ViewModels;

public sealed class SettingsGroupViewModel : ObservableViewModel
{
    public SettingsGroupViewModel(IReadOnlyList<ISettingsPageViewModel> pages)
    {
        Pages = pages;
        foreach (var page in pages.OfType<INotifyPropertyChanged>())
            page.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ISettingsPageViewModel.Group))
                    OnPropertyChanged(nameof(Title));
            };
    }

    public string Title => Pages[0].Group;
    public IReadOnlyList<ISettingsPageViewModel> Pages { get; }
}
