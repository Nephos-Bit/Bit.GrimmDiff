using System.Windows.Media;
using GrimmDiff.Gui.Commands;
using GrimmDiff.Gui.Services;
using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.UseCases;

namespace GrimmDiff.Gui.ViewModels;

public sealed class CodexAccountViewModel : ObservableViewModel, IDisposable
{
    private readonly CodexSessionService _codex;
    private readonly PreferencesStateService _preferences;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private CodexCliStatusKindModel _statusKind = CodexCliStatusKindModel.Unavailable;
    private string? _email;
    private int? _weeklyRemainingPercent;
    private DateTimeOffset _lastRefreshUtc = DateTimeOffset.MinValue;
    private string _statusMessage = "Checking Codex CLI...";
    private bool _analysisRunning;

    public CodexAccountViewModel(CodexSessionService codex, PreferencesStateService preferences)
    {
        _codex = codex;
        _preferences = preferences;
        LoginCommand = new AsyncCommand(LoginAsync, () => !_analysisRunning);
        _preferences.PropertyChanged += OnPreferencesChanged;
    }

    public AsyncCommand LoginCommand { get; }
    public string StatusMessage
    {
        get => _statusMessage;
        private set => Set(ref _statusMessage, value);
    }

    public Brush StatusBrush => (Brush)System.Windows.Application.Current.Resources[
        _statusKind == CodexCliStatusKindModel.Ready ? "Active" : "Warning"];
    public string StatusText => _statusKind == CodexCliStatusKindModel.Ready
        ? German ? "ChatGPT verbunden" : "ChatGPT connected"
        : German ? "ChatGPT nicht verbunden" : "ChatGPT not connected";
    public string AccountText => _statusKind == CodexCliStatusKindModel.Ready
        ? _email ?? (German ? "Angemeldet" : "Signed in")
        : German ? "Anmelden" : "Sign in";
    public string WeeklyQuotaText => _weeklyRemainingPercent is int remaining ? $"{remaining} %" : "–";
    public string WeeklyQuotaToolTip => _weeklyRemainingPercent is int remaining
        ? German ? $"Wöchentliches Restkontingent: {remaining} %" : $"Weekly remaining quota: {remaining}%"
        : German ? "Wöchentliches Restkontingent nicht verfügbar" : "Weekly remaining quota unavailable";
    private bool German => _preferences.Language == "Deutsch";

    internal void SetAnalysisRunning(bool value)
    {
        if (_analysisRunning == value) return;
        _analysisRunning = value;
        LoginCommand.RaiseCanExecuteChanged();
    }

    public async Task CheckAsync()
    {
        try
        {
            var result = await Task.Run(() => _codex.GetStatusAsync());
            SetStatus(result);
            if (result.IsReady) _ = RefreshUsageAsync();
        }
        catch (Exception)
        {
            SetStatus(new CodexCliStatusModel(CodexCliStatusKindModel.Unavailable, string.Empty));
        }
    }

    public async Task LoginAsync()
    {
        StatusMessage = German ? "ChatGPT-Anmeldung wird geöffnet..." : "Opening ChatGPT sign-in...";
        try
        {
            var result = await Task.Run(() => _codex.LoginAsync());
            SetStatus(result);
            if (result.IsReady) await RefreshUsageAsync(true);
        }
        catch (Exception)
        {
            SetStatus(new CodexCliStatusModel(CodexCliStatusKindModel.Unavailable, string.Empty));
        }
    }

    public async Task RefreshUsageAsync(bool force = false)
    {
        if (!force && DateTimeOffset.UtcNow - _lastRefreshUtc < TimeSpan.FromMinutes(15)) return;
        if (!await _refreshGate.WaitAsync(0)) return;
        try
        {
            if (!force && DateTimeOffset.UtcNow - _lastRefreshUtc < TimeSpan.FromMinutes(15)) return;
            var usage = await Task.Run(() => _codex.GetAccountUsageAsync());
            _lastRefreshUtc = DateTimeOffset.UtcNow;
            _email = usage.Email;
            _weeklyRemainingPercent = usage.WeeklyRemainingPercent;
            NotifyAccount();
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private void SetStatus(CodexCliStatusModel status)
    {
        _statusKind = status.Kind;
        if (!status.IsReady)
        {
            _email = null;
            _weeklyRemainingPercent = null;
        }
        StatusMessage = status.Kind switch
        {
            CodexCliStatusKindModel.Ready => German ? "Codex ist mit ChatGPT angemeldet." : "Codex is signed in with ChatGPT.",
            CodexCliStatusKindModel.Missing => German ? "Codex CLI wurde nicht gefunden." : "Codex CLI was not found.",
            CodexCliStatusKindModel.NotAuthenticated => German ? "Codex ist nicht angemeldet." : "Codex is not signed in.",
            CodexCliStatusKindModel.ApiKeyAuthentication or CodexCliStatusKindModel.UnsupportedAuthentication =>
                German ? "Codex verwendet keine ChatGPT-Anmeldung." : "Codex is not using a ChatGPT sign-in.",
            _ => German ? "Codex ist derzeit nicht verfügbar." : "Codex is currently unavailable."
        };
        OnPropertyChanged(nameof(StatusBrush));
        OnPropertyChanged(nameof(StatusText));
        NotifyAccount();
    }

    private void NotifyAccount()
    {
        OnPropertyChanged(nameof(AccountText));
        OnPropertyChanged(nameof(WeeklyQuotaText));
        OnPropertyChanged(nameof(WeeklyQuotaToolTip));
    }

    private void OnPreferencesChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PreferencesStateService.Language) or "")
        {
            OnPropertyChanged(nameof(StatusText));
            NotifyAccount();
        }
        if (e.PropertyName is nameof(PreferencesStateService.Theme) or "") OnPropertyChanged(nameof(StatusBrush));
    }

    public void Dispose()
    {
        _preferences.PropertyChanged -= OnPreferencesChanged;
        _refreshGate.Dispose();
    }
}
