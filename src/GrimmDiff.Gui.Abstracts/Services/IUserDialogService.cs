namespace GrimmDiff.Gui.Abstracts;

public interface IUserDialogService
{
    string? SelectRepository();
    string? Ask(string title, string prompt);
    bool Confirm(string title, string message);
    void ShowInformation(string title, string message);
    void ShowReleaseNotes(string title, string notes);
}