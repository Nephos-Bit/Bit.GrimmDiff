using System.Windows;
using Microsoft.Win32;

namespace GrimmDiff.App;

public sealed class WpfUserDialogService : IUserDialogService
{
    public string? SelectRepository()
    {
        var dialog = new OpenFolderDialog { Title = "Open Git repository" };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public string? Ask(string title, string prompt)
    {
        return InputDialog.Ask(title, prompt);
    }

    public bool Confirm(string title, string message)
    {
        return MessageBox.Show(message, title, MessageBoxButton.YesNo,
            MessageBoxImage.Information) == MessageBoxResult.Yes;
    }

    public void ShowInformation(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public void ShowReleaseNotes(string title, string notes)
    {
        ReleaseNotesDialog.Show(title, notes);
    }
}