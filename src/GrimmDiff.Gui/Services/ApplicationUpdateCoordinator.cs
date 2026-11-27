using System.IO;
using GrimmDiff.Gui.Abstracts.Models;
using GrimmDiff.Gui.Abstracts.Services;
using GrimmDiff.Gui.Composition;

namespace GrimmDiff.Gui.Services;

public sealed class ApplicationUpdateCoordinator(IApplicationUpdateService updates, IUserDialogService dialogs,
    PreferencesStateService preferences)
{
    public string ApplicationVersion => typeof(App).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public Task CheckOnStartupAsync() => CheckAsync(false);
    public Task CheckManuallyAsync() => CheckAsync(true);

    public async Task ShowReleaseNotesIfNeededAsync()
    {
        if (!updates.IsInstalled) return;
        var notes = ReleaseNotesReaderService.Read(Path.Combine(AppContext.BaseDirectory, "ReleaseNotes.md"));
        if (notes is null || notes.Version != ApplicationVersion ||
            notes.Version == preferences.LastShownReleaseNotesVersion) return;
        dialogs.ShowReleaseNotes(Localize($"What's new in version {notes.Version}", $"Neu in Version {notes.Version}"),
            notes.ForLanguage(preferences.Language));
        preferences.SetLastShownReleaseNotesVersion(notes.Version);
        await preferences.FlushAsync();
    }

    private async Task CheckAsync(bool showNoUpdateMessage)
    {
        var result = await updates.CheckForUpdateAsync();
        switch (result.Status)
        {
            case ApplicationUpdateStatusModel.NoUpdate when showNoUpdateMessage:
                dialogs.ShowInformation(Title, Localize("GrimmDiff is up to date.", "GrimmDiff ist aktuell."));
                break;
            case ApplicationUpdateStatusModel.NotInstalled when showNoUpdateMessage:
                dialogs.ShowInformation(Title, Localize(
                    "Updates are available after installing GrimmDiff with Setup.exe.",
                    "Updates stehen nach der Installation von GrimmDiff über Setup.exe zur Verfügung."));
                break;
            case ApplicationUpdateStatusModel.Failed when showNoUpdateMessage:
                dialogs.ShowInformation(Title, Localize(
                    "Updates could not be checked. Check your internet connection and try again.",
                    "Updates konnten nicht geprüft werden. Prüfe die Internetverbindung und versuche es erneut."));
                break;
            case ApplicationUpdateStatusModel.UpdateAvailable:
                await OfferUpdateAsync(result);
                break;
        }
    }

    private async Task OfferUpdateAsync(ApplicationUpdateCheckResultModel update)
    {
        var message = Localize(
            $"Version {update.Version} is available. Download, install, and restart GrimmDiff now?",
            $"Version {update.Version} ist verfügbar. GrimmDiff jetzt herunterladen, installieren und neu starten?");
        if (!dialogs.Confirm(Title, message)) return;
        if (await updates.DownloadAndRestartAsync() == ApplicationUpdateApplyResultModel.Failed)
            dialogs.ShowInformation(Title, Localize(
                "The update could not be installed. Please try again later.",
                "Das Update konnte nicht installiert werden. Bitte versuche es später erneut."));
    }

    private string Title => Localize("GrimmDiff update", "GrimmDiff-Update");
    private string Localize(string english, string german) => preferences.Language == "Deutsch" ? german : english;
}
