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
        return System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.YesNo,
                   System.Windows.MessageBoxImage.Information) == System.Windows.MessageBoxResult.Yes;
    }

    public void ShowInformation(string title, string message)
    {
        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    public void ShowReleaseNotes(string title, string notes)
    {
        ReleaseNotesDialog.Show(title, notes);
    }
}
