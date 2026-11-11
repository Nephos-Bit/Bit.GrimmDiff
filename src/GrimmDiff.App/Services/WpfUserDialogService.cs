using Microsoft.Win32;
namespace GrimmDiff.App;
public sealed class WpfUserDialogService : IUserDialogService { public string? SelectRepository() { var dialog = new OpenFolderDialog { Title = "Open Git repository" }; return dialog.ShowDialog() == true ? dialog.FolderName : null; } public string? Ask(string title, string prompt) => InputDialog.Ask(title, prompt); }
