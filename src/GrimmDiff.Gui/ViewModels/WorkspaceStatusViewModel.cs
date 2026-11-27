namespace GrimmDiff.Gui.ViewModels;

public sealed class WorkspaceStatusViewModel : ObservableViewModel
{
    private string _message = "Select a repository to inspect its changes.";

    public string Message
    {
        get => _message;
        set => Set(ref _message, value);
    }
}
