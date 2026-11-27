using GrimmDiff.Gui.Models;
using GrimmDiff.Gui.ViewModels;
using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.UseCases;

namespace GrimmDiff.Gui.Services;

public sealed class PreferencesStateService : ObservableViewModel
{
    private readonly PreferencesService _store;
    private readonly object _saveLock = new();
    private Task _lastSave = Task.CompletedTask;
    private AppPreferencesModel _current = AppPreferencesModel.Empty;

    public PreferencesStateService(PreferencesService store)
    {
        _store = store;
        Text = new LocalizedTextService(() => Language);
    }

    public LocalizedTextService Text { get; }
    public string Language => _current.Language;
    public string Theme => _current.Theme;
    public string Font => _current.Font;
    public bool MascotsEnabled => _current.MascotsEnabled;
    public string MascotId => _current.MascotId;
    public TicketLinkSettingsModel TicketLinks => _current.TicketLinkConfiguration;
    public IReadOnlyList<string> RecentRepositories => _current.RecentRepositories;
    public IReadOnlyList<AiRepositoryApprovalModel> Approvals => _current.Approvals;
    public bool AiRiskNoticeAccepted => _current.AiRiskNoticeAccepted;
    public string? LastShownReleaseNotesVersion => _current.LastShownReleaseNotesVersion;

    public async Task LoadAsync()
    {
        var saved = await _store.LoadAsync();
        _current = saved with
        {
            Font = saved.Font is "Cascadia Code" or "Inter" or "Verdana" ? saved.Font : "Cascadia Code",
            MascotId = MascotCatalog.Find(saved.MascotId).Id
        };
        OnPropertyChanged(string.Empty);
    }

    public void SetLanguage(string value) => Update(_current with { Language = value }, nameof(Language));
    public void SetTheme(string value) => Update(_current with { Theme = value }, nameof(Theme));
    public void SetFont(string value) => Update(_current with { Font = value }, nameof(Font));
    public void SetMascotsEnabled(bool value) => Update(_current with { MascotsEnabled = value }, nameof(MascotsEnabled));
    public void SetMascotId(string value) => Update(_current with { MascotId = MascotCatalog.Find(value).Id }, nameof(MascotId));
    public void SetTicketLinks(TicketLinkSettingsModel value) => Update(_current with { TicketLinks = value }, nameof(TicketLinks));
    public void SetAiRiskNoticeAccepted() => Update(_current with { AiRiskNoticeAccepted = true }, nameof(AiRiskNoticeAccepted));
    public void SetLastShownReleaseNotesVersion(string version) =>
        Update(_current with { LastShownReleaseNotesVersion = version }, nameof(LastShownReleaseNotesVersion));
    public void SetApprovals(IReadOnlyList<AiRepositoryApprovalModel> approvals) =>
        Update(_current with { AiRepositoryApprovals = approvals }, nameof(Approvals));
    public void AddRecent(string path) => Update(_store.AddRecent(_current, path), nameof(RecentRepositories));

    public Task FlushAsync()
    {
        lock (_saveLock) return _lastSave;
    }

    private void Update(AppPreferencesModel next, string property)
    {
        if (Equals(_current, next)) return;
        _current = next;
        OnPropertyChanged(property);
        if (property == nameof(Language)) OnPropertyChanged(nameof(Text));
        lock (_saveLock)
        {
            var snapshot = _current;
            _lastSave = _lastSave.ContinueWith(_ => _store.SaveAsync(snapshot),
                CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
        }
    }
}
