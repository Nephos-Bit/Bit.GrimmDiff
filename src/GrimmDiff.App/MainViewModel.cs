using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows.Media;
using GrimmDiff.Application;
using GrimmDiff.Domain;
using Timer = System.Timers.Timer;

namespace GrimmDiff.App;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly SemaphoreSlim _accountRefreshGate = new(1, 1);
    private readonly DiffAnalysisService _analysis;
    private readonly IRepositoryChangeMonitor _changeMonitor;
    private readonly CodexSessionService _codex;
    private readonly IUserDialogService _dialogs;
    private readonly IExternalNavigationService _navigation;
    private readonly PreferencesService _preferences;
    private readonly Timer _refreshTimer = new(500) { AutoReset = false };
    private readonly RepositoryWorkspaceService _workspace;
    private List<AiRepositoryApproval> _aiRepositoryApprovals = [];
    private bool _aiRiskNoticeAccepted;
    private CancellationTokenSource? _analysisCancellation, _diffRenderCancellation;
    private long _analysisGeneration;
    private string _analysisSourceLabel = "No analysis yet.", _aiStatusMessage = "Checking Codex CLI...";
    private string? _codexEmail;
    private CodexCliStatusKind _codexStatusKind = CodexCliStatusKind.Unavailable;
    private long _diffRenderGeneration;
    private DiffRenderDocument? _displayedDiff;
    private bool _hasMoreDiff;
    private int _initialized, _refreshRunning, _refreshPending;
    private DateTimeOffset _lastAccountRefreshUtc = DateTimeOffset.MinValue;
    private bool _loading, _isAnalyzing, _isAnalysisStale;

    private string _repositoryPath = string.Empty,
        _statusMessage = "Select a repository to inspect its changes.",
        _selectedLanguage = "English",
        _selectedTheme = "System",
        _selectedFont = "Cascadia Code";

    private CommitHistoryItem? _selectedCommit;
    private AnalysisFileViewModel? _selectedFile;
    private string? _selectedRecent;
    private int? _weeklyRemainingPercent;

    public MainViewModel(RepositoryWorkspaceService workspace, DiffAnalysisService analysis, CodexSessionService codex,
        PreferencesService preferences, IUserDialogService dialogs, IExternalNavigationService navigation,
        IRepositoryChangeMonitor changeMonitor)
    {
        _workspace = workspace;
        _analysis = analysis;
        _codex = codex;
        _preferences = preferences;
        _dialogs = dialogs;
        _navigation = navigation;
        _changeMonitor = changeMonitor;
        _refreshTimer.Elapsed += OnRefreshTimerElapsed;
        OpenRepositoryCommand = new AsyncCommand(OpenRepositoryAsync);
        CloneCommand = new AsyncCommand(CloneAsync);
        ShowCurrentCommand = new AsyncCommand(ShowCurrentAsync);
        LoadMoreDiffCommand = new AsyncCommand(LoadMoreDiffAsync, () => HasMoreDiff);
        SelectFileCommand = new DelegateCommand<AnalysisFileViewModel>(SelectFile);
        OpenUrlCommand = new DelegateCommand<string>(OpenUrl);
        AnalyzeWithAiCommand = new AsyncCommand(AnalyzeWithAiAsync,
            () => !string.IsNullOrWhiteSpace(RepositoryPath) && !IsAnalyzing);
        LoginToCodexCommand = new AsyncCommand(LoginToCodexAsync, () => !IsAnalyzing);
        CancelAnalysisCommand = new AsyncCommand(CancelAnalysisAsync, () => IsAnalyzing);
        ShowAiRiskNoticeCommand = new AsyncCommand(async () => _ = await ShowAiRiskNoticeAsync());
    }

    public BulkObservableCollection<RecentRepositoryItem> RecentRepositories { get; } = [];
    public BulkObservableCollection<CommitHistoryItem> History { get; } = [];
    public BulkObservableCollection<AnalysisGroupViewModel> Groups { get; } = [];

    public DiffRenderDocument? DisplayedDiff
    {
        get => _displayedDiff;
        private set => Set(ref _displayedDiff, value);
    }

    public bool HasMoreDiff
    {
        get => _hasMoreDiff;
        private set
        {
            if (Set(ref _hasMoreDiff, value)) LoadMoreDiffCommand.RaiseCanExecuteChanged();
        }
    }

    public IReadOnlyList<string> Languages { get; } = ["English", "Deutsch"];
    public IReadOnlyList<string> Themes { get; } = ["System", "Dark", "Light"];
    public IReadOnlyList<string> Fonts { get; } = ["Cascadia Code", "Verdana"];
    public LocalizedText Text => new(() => SelectedLanguage);
    public AsyncCommand OpenRepositoryCommand { get; }
    public AsyncCommand CloneCommand { get; }
    public AsyncCommand ShowCurrentCommand { get; }
    public AsyncCommand LoadMoreDiffCommand { get; }
    public AsyncCommand AnalyzeWithAiCommand { get; }
    public AsyncCommand LoginToCodexCommand { get; }
    public AsyncCommand CancelAnalysisCommand { get; }
    public AsyncCommand ShowAiRiskNoticeCommand { get; }
    public DelegateCommand<AnalysisFileViewModel> SelectFileCommand { get; }
    public DelegateCommand<string> OpenUrlCommand { get; }

    public string RepositoryPath
    {
        get => _repositoryPath;
        private set
        {
            if (Set(ref _repositoryPath, value))
            {
                OnPropertyChanged(nameof(RepositoryName));
                AnalyzeWithAiCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string RepositoryName => string.IsNullOrWhiteSpace(RepositoryPath)
        ? string.Empty
        : Path.GetFileName(Path.TrimEndingDirectorySeparator(RepositoryPath));

    public string StatusMessage
    {
        get => _statusMessage;
        private set => Set(ref _statusMessage, value);
    }

    public string AnalysisSourceLabel
    {
        get => _analysisSourceLabel;
        private set => Set(ref _analysisSourceLabel, value);
    }

    public string AiStatusMessage
    {
        get => _aiStatusMessage;
        private set => Set(ref _aiStatusMessage, value);
    }

    public Brush CodexStatusBrush =>
        (Brush)System.Windows.Application.Current.Resources[
            _codexStatusKind == CodexCliStatusKind.Ready ? "Active" : "Warning"];

    public string CodexStatusText => _codexStatusKind == CodexCliStatusKind.Ready
        ?
        SelectedLanguage == "Deutsch" ? "ChatGPT verbunden" : "ChatGPT connected"
        : SelectedLanguage == "Deutsch"
            ? "ChatGPT nicht verbunden"
            : "ChatGPT not connected";

    public string CodexAccountText => _codexStatusKind == CodexCliStatusKind.Ready
        ? _codexEmail ?? (SelectedLanguage == "Deutsch" ? "Angemeldet" : "Signed in")
        : SelectedLanguage == "Deutsch"
            ? "Anmelden"
            : "Sign in";

    public string WeeklyQuotaText => _weeklyRemainingPercent is int remaining
        ? SelectedLanguage == "Deutsch" ? $"WOCHE · {remaining} % VERBLEIBEND" : $"WEEK · {remaining}% LEFT"
        : SelectedLanguage == "Deutsch"
            ? "WOCHE · –"
            : "WEEK · –";

    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        private set
        {
            if (Set(ref _isAnalyzing, value))
            {
                AnalyzeWithAiCommand.RaiseCanExecuteChanged();
                LoginToCodexCommand.RaiseCanExecuteChanged();
                CancelAnalysisCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsAnalysisStale
    {
        get => _isAnalysisStale;
        private set => Set(ref _isAnalysisStale, value);
    }

    public string AnalysisProgressText => SelectedLanguage == "Deutsch"
        ? "GPT-6 Luna analysiert die Änderungen..."
        : "GPT-6 Luna is analysing the changes...";

    public string AnalyzeButtonText =>
        SelectedLanguage == "Deutsch" ? "Mit GPT-6 Luna analysieren" : "Analyse with GPT-6 Luna";

    public string LoginButtonText => SelectedLanguage == "Deutsch" ? "Mit ChatGPT anmelden" : "Sign in with ChatGPT";
    public string CancelButtonText => SelectedLanguage == "Deutsch" ? "Abbrechen" : "Cancel";

    public string AiDisclosureText => SelectedLanguage == "Deutsch"
        ? "Die CLI läuft lokal; gelesene Repository-Inhalte werden über den angemeldeten ChatGPT-Workspace verarbeitet."
        : "The CLI runs locally; repository content it reads is processed through the signed-in ChatGPT workspace.";

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (Set(ref _selectedLanguage, value))
            {
                OnPropertyChanged(nameof(Text));
                OnPropertyChanged(nameof(IsEnglish));
                OnPropertyChanged(nameof(IsGerman));
                OnPropertyChanged(nameof(AnalyzeButtonText));
                OnPropertyChanged(nameof(LoginButtonText));
                OnPropertyChanged(nameof(CancelButtonText));
                OnPropertyChanged(nameof(AnalysisProgressText));
                OnPropertyChanged(nameof(AiDisclosureText));
                OnPropertyChanged(nameof(CodexStatusText));
                OnPropertyChanged(nameof(CodexAccountText));
                OnPropertyChanged(nameof(WeeklyQuotaText));
                SavePreferences();
                _ = LoadAnalysisAsync(true);
            }
        }
    }

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (Set(ref _selectedTheme, value))
            {
                OnPropertyChanged(nameof(IsSystemTheme));
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(IsLightTheme));
                ThemeService.Apply(value);
                OnPropertyChanged(nameof(CodexStatusBrush));
                _ = ShowFileAsync(SelectedFile);
                SavePreferences();
            }
        }
    }

    public string SelectedFont
    {
        get => _selectedFont;
        set
        {
            if (Set(ref _selectedFont, value))
            {
                OnPropertyChanged(nameof(IsCascadiaCode));
                OnPropertyChanged(nameof(IsVerdana));
                ApplyFont(value);
                SavePreferences();
            }
        }
    }

    public bool IsEnglish
    {
        get => SelectedLanguage == "English";
        set
        {
            if (value) SelectedLanguage = "English";
        }
    }

    public bool IsGerman
    {
        get => SelectedLanguage == "Deutsch";
        set
        {
            if (value) SelectedLanguage = "Deutsch";
        }
    }

    public bool IsSystemTheme
    {
        get => SelectedTheme == "System";
        set
        {
            if (value) SelectedTheme = "System";
        }
    }

    public bool IsDarkTheme
    {
        get => SelectedTheme == "Dark";
        set
        {
            if (value) SelectedTheme = "Dark";
        }
    }

    public bool IsLightTheme
    {
        get => SelectedTheme == "Light";
        set
        {
            if (value) SelectedTheme = "Light";
        }
    }

    public bool IsCascadiaCode
    {
        get => SelectedFont == "Cascadia Code";
        set
        {
            if (value) SelectedFont = "Cascadia Code";
        }
    }

    public bool IsVerdana
    {
        get => SelectedFont == "Verdana";
        set
        {
            if (value) SelectedFont = "Verdana";
        }
    }

    public CommitHistoryItem? SelectedCommit
    {
        get => _selectedCommit;
        set
        {
            if (Set(ref _selectedCommit, value))
            {
                OnPropertyChanged(nameof(ViewTitle));
                _ = LoadAnalysisAsync(true);
            }
        }
    }

    public AnalysisFileViewModel? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (Set(ref _selectedFile, value))
            {
                _ = ShowFileAsync(value);
                OnPropertyChanged(nameof(SelectedAnalysisTitle));
                OnPropertyChanged(nameof(SelectedAnalysisDescription));
            }
        }
    }

    public string SelectedAnalysisTitle => SelectedFile?.GroupTitle ?? Groups.FirstOrDefault()?.Title ?? string.Empty;

    public string SelectedAnalysisDescription =>
        SelectedFile?.GroupDescription ?? Groups.FirstOrDefault()?.Description ?? string.Empty;

    public RecentRepositoryItem? SelectedRecent
    {
        get => RecentRepositories.FirstOrDefault(item => item.Path == _selectedRecent);
        set
        {
            if (Set(ref _selectedRecent, value?.Path) && value is not null) _ = OpenRepositoryAsync(value.Path);
        }
    }

    public string ViewTitle => SelectedCommit is null
        ? "Current changes"
        : $"{SelectedCommit.Commit.Subject} · {SelectedCommit.Commit.Id[..Math.Min(8, SelectedCommit.Commit.Id.Length)]}";

    public void Dispose()
    {
        _analysisCancellation?.Cancel();
        _analysisCancellation?.Dispose();
        _diffRenderCancellation?.Cancel();
        _diffRenderCancellation?.Dispose();
        _changeMonitor.Dispose();
        _refreshTimer.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task InitializeAsync()
    {
        if (Interlocked.Exchange(ref _initialized, 1) != 0) return;
        var saved = await _preferences.LoadAsync();
        _selectedLanguage = saved.Language;
        _selectedTheme = saved.Theme;
        _selectedFont = saved.Font is "Cascadia Code" or "Verdana" ? saved.Font : "Cascadia Code";
        _aiRepositoryApprovals = saved.Approvals.ToList();
        _aiRiskNoticeAccepted = saved.AiRiskNoticeAccepted;
        ThemeService.Apply(_selectedTheme);
        ApplyFont(_selectedFont);
        RecentRepositories.ReplaceAll(saved.RecentRepositories.Select(item => new RecentRepositoryItem(item)));
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(SelectedTheme));
        OnPropertyChanged(nameof(SelectedFont));
        if (!_aiRiskNoticeAccepted && !await ShowAiRiskNoticeAsync())
        {
            System.Windows.Application.Current.Shutdown();
            return;
        }

        await Task.Delay(750);
        await CheckCodexAsync();
    }

    #region Private

    private async Task OpenRepositoryAsync()
    {
        var path = _dialogs.SelectRepository();
        if (!string.IsNullOrWhiteSpace(path)) await OpenRepositoryAsync(path);
    }

    private async Task OpenRepositoryAsync(string path)
    {
        if (!await _workspace.IsGitAvailableAsync())
        {
            StatusMessage = "Git is not installed or unavailable on PATH.";
            return;
        }

        RepositoryPath = path;
        AddRecent(path);
        _changeMonitor.Watch(path, ScheduleRefresh);
        await RefreshAsync(false);
    }

    private async Task CloneAsync()
    {
        var source = _dialogs.Ask("Clone repository", "Repository URL or local source:");
        if (string.IsNullOrWhiteSpace(source)) return;
        var destination = _dialogs.Ask("Clone repository", "Destination folder:");
        if (string.IsNullOrWhiteSpace(destination)) return;
        var result = await _workspace.CloneAsync(source, destination);
        StatusMessage = result.IsSuccess ? "Repository cloned." : result.ErrorMessage;
        if (result.IsSuccess) await OpenRepositoryAsync(destination);
    }

    private async Task ShowCurrentAsync()
    {
        if (SelectedCommit is null) await LoadAnalysisAsync(true);
        else SelectedCommit = null;
    }

    private async Task RefreshAsync(bool analyzeWithAi)
    {
        if (string.IsNullOrWhiteSpace(RepositoryPath) || _loading) return;
        _loading = true;
        var refreshAnalysis = false;
        try
        {
            var repositoryPath = RepositoryPath;
            var snapshot = await _workspace.OpenAsync(repositoryPath);
            History.ReplaceAll(snapshot.History.Select(commit => new CommitHistoryItem(commit)));
            StatusMessage = snapshot.State == RepositoryState.Ready
                ? $"{snapshot.Changes.Count} current file changes."
                : $"Git state: {snapshot.State}. Resolve it before editing.";
            refreshAnalysis = SelectedCommit is null || analyzeWithAi;
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            _loading = false;
        }

        if (refreshAnalysis) await LoadAnalysisAsync(analyzeWithAi);
    }

    private async Task LoadAnalysisAsync(bool analyzeWithAi)
    {
        if (string.IsNullOrWhiteSpace(RepositoryPath)) return;
        var generation = Interlocked.Increment(ref _analysisGeneration);
        _analysisCancellation?.Cancel();
        _analysisCancellation?.Dispose();
        _analysisCancellation = new CancellationTokenSource();
        var cancellationToken = _analysisCancellation.Token;
        var repositoryPath = RepositoryPath;
        var commitId = SelectedCommit?.Commit.Id;
        var language = SelectedLanguage == "Deutsch" ? "de" : "en";
        IsAnalyzing = analyzeWithAi;
        if (analyzeWithAi) StatusMessage = AnalysisProgressText;
        try
        {
            var analysisView =
                await _analysis.AnalyzeAsync(new DiffAnalysisQuery(repositoryPath, commitId, language, analyzeWithAi),
                    cancellationToken);
            // Grouping can concatenate large diffs. Keep that allocation off the dispatcher.
            var prepared = (Analysis: analysisView.Result,
                Groups: await Task.Run(
                    () => analysisView.Result.Groups.Select(group => new AnalysisGroupViewModel(group)).ToList(),
                    cancellationToken));
            if (generation != Interlocked.Read(ref _analysisGeneration)) return;
            var result = prepared.Analysis;
            var previousSelection = SelectedFile;
            Groups.ReplaceAll(prepared.Groups);
            var firstFile = Groups.SelectMany(group => group.Files).FirstOrDefault();
            var restoredSelection = previousSelection is null
                ? null
                : Groups.SelectMany(group => group.Files).FirstOrDefault(file =>
                    file.Path.Equals(previousSelection.Path, StringComparison.OrdinalIgnoreCase) &&
                    file.GroupTitle.Equals(previousSelection.GroupTitle, StringComparison.Ordinal));
            var selectedFile = restoredSelection ?? firstFile;
            if (!ReferenceEquals(SelectedFile, selectedFile)) SelectedFile = selectedFile;
            OnPropertyChanged(nameof(SelectedAnalysisTitle));
            OnPropertyChanged(nameof(SelectedAnalysisDescription));
            OnPropertyChanged(nameof(ViewTitle));
            IsAnalysisStale = !analyzeWithAi;
            AnalysisSourceLabel = result.Source == DiffAnalysisSource.Codex
                ? SelectedLanguage == "Deutsch" ? "GPT-6 Luna · KI-Gruppierung" : "GPT-6 Luna · AI grouping"
                : analyzeWithAi
                    ? SelectedLanguage == "Deutsch" ? "Regelbasierter Fallback" : "Rule-based fallback"
                    : SelectedLanguage == "Deutsch"
                        ? "Regelbasierte Vorschau · KI-Neuanalyse verfügbar"
                        : "Rule-based preview · AI refresh available";
            var summary = result.Hunks.Count == 0
                ? SelectedLanguage == "Deutsch" ? "Keine analysierbaren Änderungen." : "No textual changes to analyse."
                : SelectedLanguage == "Deutsch"
                    ? $"{result.Hunks.Count} geänderte Abschnitte wurden in {result.Groups.Count} Arbeitsschritte gruppiert."
                    : $"{result.Hunks.Count} changed sections grouped into {result.Groups.Count} work steps.";
            StatusMessage = string.IsNullOrWhiteSpace(result.Diagnostic) ? summary : $"{result.Diagnostic} {summary}";
        }
        catch (OperationCanceledException)
        {
            if (generation == Interlocked.Read(ref _analysisGeneration))
                StatusMessage = SelectedLanguage == "Deutsch" ? "Analyse abgebrochen." : "Analysis cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            if (generation == Interlocked.Read(ref _analysisGeneration))
            {
                IsAnalyzing = false;
                _analysisCancellation?.Dispose();
                _analysisCancellation = null;
            }
        }
    }

    private async Task AnalyzeWithAiAsync()
    {
        var repositoryId = RepositoryIdentity(RepositoryPath);
        if (!_aiRepositoryApprovals.Any(approval => approval.RepositoryId == repositoryId))
        {
            var confirmation = _dialogs.Ask("AI analysis",
                "Repository diffs, including untracked files except protected key and credential files, will be processed by your signed-in ChatGPT workspace. Type ALLOW to approve this repository.");
            if (!string.Equals(confirmation, "ALLOW", StringComparison.Ordinal))
            {
                StatusMessage = "AI analysis was not approved.";
                return;
            }

            _aiRepositoryApprovals.RemoveAll(approval =>
                approval.RepositoryId.StartsWith(Path.GetFullPath(RepositoryPath), StringComparison.OrdinalIgnoreCase));
            _aiRepositoryApprovals.Add(new AiRepositoryApproval(repositoryId, "Pending trusted CLI",
                "Pending verification", DateTimeOffset.UtcNow));
            SavePreferences();
        }

        await LoadAnalysisAsync(true);
    }

    private Task<bool> ShowAiRiskNoticeAsync()
    {
        var german = SelectedLanguage == "Deutsch";
        var confirmation = _dialogs.Ask(german ? "KI-Risikohinweis" : "AI risk notice", german
            ? "Die Anwendung hat Zugriff auf das von dir geöffnete lokale Git-Repository. Nach deiner Freigabe erhält die KI ausschließlich staged-, Arbeitsbaum- und nicht versionierte Diffs. Geschützte Schlüssel- und Zugangsdaten-Dateien werden lokal analysiert und nicht übertragen. Die KI wird im angemeldeten ChatGPT-Workspace ausgeführt; sie hat keinen freien Lesezugriff auf weitere Repository-Dateien.\n\nTippe ACCEPT, um die Anwendung zu verwenden."
            : "The application can access the local Git repository you open. After your approval, AI receives only staged, working-tree, and untracked diffs. Protected key and credential files stay local. AI runs in your signed-in ChatGPT workspace and has no unrestricted access to other repository files.\n\nType ACCEPT to use the application.");
        if (!string.Equals(confirmation, "ACCEPT", StringComparison.Ordinal)) return Task.FromResult(false);
        if (!_aiRiskNoticeAccepted)
        {
            _aiRiskNoticeAccepted = true;
            SavePreferences();
        }

        return Task.FromResult(true);
    }

    private Task CancelAnalysisAsync()
    {
        _analysisCancellation?.Cancel();
        return Task.CompletedTask;
    }

    private async Task CheckCodexAsync()
    {
        try
        {
            var status = await Task.Run(() => _codex.GetStatusAsync());
            SetCodexStatus(status);
            if (status.IsReady) _ = RefreshCodexAccountAsync();
        }
        catch (Exception)
        {
            SetCodexStatus(new CodexCliStatus(CodexCliStatusKind.Unavailable, string.Empty));
        }
    }

    private async Task LoginToCodexAsync()
    {
        AiStatusMessage = SelectedLanguage == "Deutsch"
            ? "ChatGPT-Anmeldung wird geöffnet..."
            : "Opening ChatGPT sign-in...";
        try
        {
            var status = await Task.Run(() => _codex.LoginAsync());
            SetCodexStatus(status);
            if (status.IsReady) await RefreshCodexAccountAsync(true);
        }
        catch (Exception)
        {
            SetCodexStatus(new CodexCliStatus(CodexCliStatusKind.Unavailable, string.Empty));
        }
    }

    private async Task RefreshCodexAccountAsync(bool force = false)
    {
        if (!force && DateTimeOffset.UtcNow - _lastAccountRefreshUtc < TimeSpan.FromMinutes(15)) return;
        if (!await _accountRefreshGate.WaitAsync(0)) return;
        try
        {
            if (!force && DateTimeOffset.UtcNow - _lastAccountRefreshUtc < TimeSpan.FromMinutes(15)) return;
            var usage = await Task.Run(() => _codex.GetAccountUsageAsync());
            _lastAccountRefreshUtc = DateTimeOffset.UtcNow;
            _codexEmail = usage.Email;
            _weeklyRemainingPercent = usage.WeeklyRemainingPercent;
            OnPropertyChanged(nameof(CodexAccountText));
            OnPropertyChanged(nameof(WeeklyQuotaText));
        }
        finally
        {
            _accountRefreshGate.Release();
        }
    }

    private void SetCodexStatus(CodexCliStatus status)
    {
        _codexStatusKind = status.Kind;
        if (!status.IsReady)
        {
            _codexEmail = null;
            _weeklyRemainingPercent = null;
        }

        AiStatusMessage = LocalizeCodexStatus(status);
        OnPropertyChanged(nameof(CodexStatusBrush));
        OnPropertyChanged(nameof(CodexStatusText));
        OnPropertyChanged(nameof(CodexAccountText));
        OnPropertyChanged(nameof(WeeklyQuotaText));
    }

    private string LocalizeCodexStatus(CodexCliStatus status)
    {
        return status.Kind switch
        {
            CodexCliStatusKind.Ready => SelectedLanguage == "Deutsch"
                ? "Codex ist mit ChatGPT angemeldet."
                : "Codex is signed in with ChatGPT.",
            CodexCliStatusKind.Missing => SelectedLanguage == "Deutsch"
                ? "Codex CLI wurde nicht gefunden."
                : "Codex CLI was not found.",
            CodexCliStatusKind.NotAuthenticated => SelectedLanguage == "Deutsch"
                ? "Codex ist nicht angemeldet."
                : "Codex is not signed in.",
            CodexCliStatusKind.ApiKeyAuthentication or CodexCliStatusKind.UnsupportedAuthentication =>
                SelectedLanguage == "Deutsch"
                    ? "Codex verwendet keine ChatGPT-Anmeldung."
                    : "Codex is not using a ChatGPT sign-in.",
            _ => SelectedLanguage == "Deutsch"
                ? "Codex ist derzeit nicht verfügbar."
                : "Codex is currently unavailable."
        };
    }

    private void SelectFile(AnalysisFileViewModel file)
    {
        SelectedFile = file;
    }

    private void OpenUrl(string url)
    {
        _navigation.Open(url);
    }

    private async Task ShowFileAsync(AnalysisFileViewModel? file)
    {
        var generation = Interlocked.Increment(ref _diffRenderGeneration);
        _diffRenderCancellation?.Cancel();
        _diffRenderCancellation?.Dispose();
        _diffRenderCancellation = new CancellationTokenSource();
        var cancellationToken = _diffRenderCancellation.Token;
        if (file is null)
        {
            PublishDiffDocument(null);
            return;
        }

        if (file.RenderDocument is not null)
        {
            PublishDiffDocument(file.RenderDocument);
            return;
        }

        try
        {
            var page = await Task.Run(
                () => DiffRenderBuilder.CreatePage(file.Hunks, cancellationToken: cancellationToken),
                cancellationToken);
            if (generation != Interlocked.Read(ref _diffRenderGeneration) ||
                !ReferenceEquals(file, SelectedFile)) return;
            file.RenderDocument = DiffRenderDocument.FromPage(page);
            PublishDiffDocument(file.RenderDocument);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadMoreDiffAsync()
    {
        var file = SelectedFile;
        var document = file?.RenderDocument;
        if (file is null || document?.NextPage is null) return;
        var generation = Interlocked.Increment(ref _diffRenderGeneration);
        _diffRenderCancellation?.Cancel();
        _diffRenderCancellation?.Dispose();
        _diffRenderCancellation = new CancellationTokenSource();
        var cancellationToken = _diffRenderCancellation.Token;
        try
        {
            var page = await Task.Run(
                () => DiffRenderBuilder.CreatePage(file.Hunks, document.NextPage, cancellationToken),
                cancellationToken);
            if (generation != Interlocked.Read(ref _diffRenderGeneration) || !ReferenceEquals(file, SelectedFile) ||
                !ReferenceEquals(document, file.RenderDocument)) return;
            file.RenderDocument = document.Append(page);
            PublishDiffDocument(file.RenderDocument);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void PublishDiffDocument(DiffRenderDocument? document)
    {
        DisplayedDiff = document;
        HasMoreDiff = document?.NextPage is not null;
    }

    private void ScheduleRefresh(string path)
    {
        if (IsGeneratedPath(path)) return;
        _analysisCancellation?.Cancel();
        Interlocked.Exchange(ref _refreshPending, 1);
        _refreshTimer.Stop();
        _refreshTimer.Start();
    }

    private static bool IsGeneratedPath(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (segments.Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                                    segment.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                                    segment.Equals("node_modules", StringComparison.OrdinalIgnoreCase) ||
                                    segment.Equals(".vs", StringComparison.OrdinalIgnoreCase))) return true;
        var gitIndex = Array.FindIndex(segments, segment => segment.Equals(".git", StringComparison.OrdinalIgnoreCase));
        if (gitIndex < 0 || gitIndex + 1 >= segments.Length) return gitIndex >= 0;
        var gitEntry = segments[gitIndex + 1];
        return !gitEntry.Equals("HEAD", StringComparison.OrdinalIgnoreCase)
               && !gitEntry.Equals("refs", StringComparison.OrdinalIgnoreCase)
               && !gitEntry.Equals("packed-refs", StringComparison.OrdinalIgnoreCase);
    }

    private void OnRefreshTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(ProcessRefreshQueueAsync);
    }

    private async Task ProcessRefreshQueueAsync()
    {
        if (Interlocked.CompareExchange(ref _refreshRunning, 1, 0) != 0) return;
        try
        {
            do
            {
                Interlocked.Exchange(ref _refreshPending, 0);
                await RefreshAsync(false);
            } while (Interlocked.Exchange(ref _refreshPending, 0) != 0);
        }
        finally
        {
            Interlocked.Exchange(ref _refreshRunning, 0);
        }
    }

    private static void ApplyFont(string font)
    {
        System.Windows.Application.Current.Resources["AppFontFamily"] = new FontFamily(font);
    }

    private void AddRecent(string path)
    {
        var preferences =
            _preferences.AddRecent(
                new AppPreferences(RecentRepositories.Select(item => item.Path).ToList(), SelectedTheme,
                    SelectedLanguage, SelectedFont), path);
        RecentRepositories.ReplaceAll(preferences.RecentRepositories.Select(item => new RecentRepositoryItem(item)));
        SavePreferences();
    }

    private void SavePreferences()
    {
        _ = _preferences.SaveAsync(new AppPreferences(RecentRepositories.Select(item => item.Path).ToList(),
            SelectedTheme, SelectedLanguage, SelectedFont, _aiRepositoryApprovals, _aiRiskNoticeAccepted));
    }

    private static string RepositoryIdentity(string repositoryPath)
    {
        var root = Path.GetFullPath(repositoryPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var config = Path.Combine(root, ".git", "config");
        var configHash = File.Exists(config)
            ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(config)))
            : "NO_CONFIG";
        return $"{root}|{configHash}";
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? property = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(property);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? property = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }

    #endregion
}

public sealed class AnalysisGroupViewModel
{
    public AnalysisGroupViewModel(WorkStepGroup group)
    {
        Title = group.Title;
        Description = group.Description;
        Files = group.Hunks.GroupBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase).Select(x =>
            new AnalysisFileViewModel(x.Key, x.ToList(), group.Title, group.Description)).ToList();
    }

    public string Title { get; }
    public string Description { get; }
    public IReadOnlyList<AnalysisFileViewModel> Files { get; }
}

public sealed class AnalysisFileViewModel
{
    public AnalysisFileViewModel(string path, IReadOnlyList<DiffHunk> hunks, string groupTitle, string groupDescription)
    {
        (Path, Hunks, GroupTitle, GroupDescription) = (path, hunks, groupTitle, groupDescription);
    }

    public string Path { get; }
    public IReadOnlyList<DiffHunk> Hunks { get; }
    public string GroupTitle { get; }
    public string GroupDescription { get; }
    public DiffRenderDocument? RenderDocument { get; internal set; }

    public override string ToString()
    {
        return Path;
    }
}

public sealed record RecentRepositoryItem(string Path)
{
    public string Name => System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(Path));

    public override string ToString()
    {
        return Name;
    }
}

public abstract record CommitSubjectPart(string Text);

public sealed record PlainCommitSubjectPart(string Text) : CommitSubjectPart(Text);

public sealed record JiraCommitSubjectPart(string Text, string Url) : CommitSubjectPart(Text);

public sealed class CommitHistoryItem
{
    private static readonly Regex JiraIssuePrefix =
        new(@"^\[(?<issue>[A-Za-z][A-Za-z0-9]*-\d+)\]", RegexOptions.Compiled);

    public CommitHistoryItem(CommitInfo commit)
    {
        Commit = commit;
        var match = JiraIssuePrefix.Match(commit.Subject);
        SubjectParts = match.Success
            ?
            [
                new JiraCommitSubjectPart(match.Value, $"https://jira/browse/{match.Groups["issue"].Value}"),
                new PlainCommitSubjectPart(commit.Subject[match.Length..])
            ]
            : [new PlainCommitSubjectPart(commit.Subject)];
    }

    public CommitInfo Commit { get; }
    public IReadOnlyList<CommitSubjectPart> SubjectParts { get; }
}

public sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotifications;

    public void ReplaceAll(IEnumerable<T> values)
    {
        _suppressNotifications = true;
        try
        {
            ClearItems();
            foreach (var value in values) Add(value);
        }
        finally
        {
            _suppressNotifications = false;
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (!_suppressNotifications) base.OnPropertyChanged(e);
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotifications) base.OnCollectionChanged(e);
    }
}