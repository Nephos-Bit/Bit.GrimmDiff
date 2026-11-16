namespace GrimmDiff.Gui;

public sealed class SettingsGroupViewModel(string title, IReadOnlyList<ISettingsPageViewModel> pages)
{
    public string Title { get; } = title;
    public IReadOnlyList<ISettingsPageViewModel> Pages { get; } = pages;
}
