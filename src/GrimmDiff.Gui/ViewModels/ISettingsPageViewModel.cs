namespace GrimmDiff.Gui.ViewModels;

public interface ISettingsPageViewModel
{
    string Id { get; }
    string Group { get; }
    string Title { get; }
    int Order { get; }
    bool IsSelected { get; set; }
}
