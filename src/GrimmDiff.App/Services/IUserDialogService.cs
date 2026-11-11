namespace GrimmDiff.App;

public interface IUserDialogService
{
    string? SelectRepository();
    string? Ask(string title, string prompt);
}