using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using GrimmDiff.Logic;
using GrimmDiff.Logic.Abstracts;
using Timer = System.Timers.Timer;

namespace GrimmDiff.Gui;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly SemaphoreSlim _accountRefreshGate = new(1, 1);
    private readonly DiffAnalysisService _analysis;
    private readonly IRepositoryChangeMonitorService _changeMonitor;
    private readonly CodexSessionService _codex;
    private readonly IUserDialogService _dialogs;
    private readonly IExternalNavigationService _navigation;
    private readonly PreferencesService _preferences;
    private readonly Timer _refreshTimer = new(500) { AutoReset = false };
    private readonly IApplicationUpdateService _updates;
    private readonly RepositoryWorkspaceService _workspace;
    private List<AiRepositoryApprovalModel> _aiRepositoryApprovals = [];
    private bool _aiRiskNoticeAccepted;
    private CancellationTokenSource? _analysisCancellation, _diffRenderCancellation;
    private long _analysisGeneration;
    private string _analysisSourceLabel = "No analysis yet.", _aiStatusMessage = "Checking Codex CLI...";
    private string? _codexEmail;
    private CodexCliStatusKindModel _codexStatusKind = CodexCliStatusKindModel.Unavailable;
    private long _diffRenderGeneration;
    private DiffRenderDocumentModel? _displayedDiff;
    private bool _hasMoreDiff;
    private int _initialized, _refreshRunning, _refreshPending;
    private DateTimeOffset _lastAccountRefreshUtc = DateTimeOffset.MinValue;
    private string? _lastShownReleaseNotesVersion;
    private bool _loading, _isAnalyzing, _isAnalysisStale, _isRepositoryOperation;
    private bool _isBranchMenuOpen;
    private bool _isRepositoryPanelExpanded = true;
    private GridLength _repositoryPanelColumnWidth = new(300);
    private GridLength _repositoryPanelExpandedWidth = new(300);

    private string _repositoryPath = string.Empty,
        _currentBranch = string.Empty,
        _statusMessage = "Select a repository to inspect its changes.",
        _selectedLanguage = "English",
        _selectedTheme = "System",
        _selectedFont = "Cascadia Code";

    private CommitHistoryItemModel? _selectedCommit;
    private AnalysisFileViewModel? _selectedFile;
    private string? _selectedRecent;
    private int? _weeklyRemainingPercent;

    public MainViewModel(RepositoryWorkspaceService workspace, DiffAnalysisService analysis, CodexSessionService codex,
        PreferencesService preferences, IUserDialogService dialogs, IExternalNavigationService navigation,
        IRepositoryChangeMonitorService changeMonitor, IApplicationUpdateService updates)
    {
        _workspace = workspace;
        _analysis = analysis;
        _codex = codex;
        _preferences = preferences;
        Text = new LocalizedTextService(() => SelectedLanguage);
        _dialogs = dialogs;
        _navigation = navigation;
        _changeMonitor = changeMonitor;
        _updates = updates;
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
        RevokeAiApprovalCommand =
            new AsyncCommand(RevokeAiApprovalAsync, () => !string.IsNullOrWhiteSpace(RepositoryPath));
        PullCommand = new AsyncCommand(PullAsync, CanRunRepositoryOperation);
        SwitchBranchCommand = new AsyncCommandOfT<BranchInfoModel>(SwitchBranchAsync,
            branch => CanRunRepositoryOperation() && !branch.IsCurrent);
        CheckForUpdatesCommand = new AsyncCommand(CheckForUpdatesManuallyAsync);
        OpenBranchMenuCommand = new DelegateCommand<object>(_ => IsBranchMenuOpen = true);
        ToggleRepositoryPanelCommand = new DelegateCommand<object>(_ => IsRepositoryPanelExpanded = !IsRepositoryPanelExpanded);
    }

    public BulkObservableCollectionModel<RecentRepositoryItemModel> RecentRepositories { get; } = [];
    public BulkObservableCollectionModel<CommitHistoryItemModel> History { get; } = [];
    public BulkObservableCollectionModel<AnalysisGroupViewModel> Groups { get; } = [];
    public BulkObservableCollectionModel<BranchInfoModel> Branches { get; } = [];
    public string ApplicationVersion => typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public DiffRenderDocumentModel? DisplayedDiff
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
    public IReadOnlyList<string> Fonts { get; } = ["Cascadia Code", "Inter", "Verdana"];
    public LocalizedTextService Text { get; }

    public AsyncCommand OpenRepositoryCommand { get; }
    public AsyncCommand CloneCommand { get; }
    public AsyncCommand ShowCurrentCommand { get; }
    public AsyncCommand LoadMoreDiffCommand { get; }
    public AsyncCommand AnalyzeWithAiCommand { get; }
    public AsyncCommand LoginToCodexCommand { get; }
    public AsyncCommand CancelAnalysisCommand { get; }
    public AsyncCommand ShowAiRiskNoticeCommand { get; }
    public AsyncCommand RevokeAiApprovalCommand { get; }
    public AsyncCommand PullCommand { get; }
    public AsyncCommandOfT<BranchInfoModel> SwitchBranchCommand { get; }
    public AsyncCommand CheckForUpdatesCommand { get; }
    public DelegateCommand<object> OpenBranchMenuCommand { get; }
    public DelegateCommand<object> ToggleRepositoryPanelCommand { get; }
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
                OnPropertyChanged(nameof(HasRepository));
                AnalyzeWithAiCommand.RaiseCanExecuteChanged();
                PullCommand.RaiseCanExecuteChanged();
                SwitchBranchCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsBranchMenuOpen
    {
        get => _isBranchMenuOpen;
        set => Set(ref _isBranchMenuOpen, value);
    }

    public bool IsRepositoryPanelExpanded
    {
        get => _isRepositoryPanelExpanded;
        set
        {
            if (!Set(ref _isRepositoryPanelExpanded, value)) return;
            RepositoryPanelColumnWidth = value ? _repositoryPanelExpandedWidth : new GridLength(0);
        }
    }

    public GridLength RepositoryPanelColumnWidth
    {
        get => _repositoryPanelColumnWidth;
        set
        {
            if (!Set(ref _repositoryPanelColumnWidth, value)) return;
            if (value.Value > 0) _repositoryPanelExpandedWidth = value;
        }
    }

    public string RepositoryName => string.IsNullOrWhiteSpace(RepositoryPath)
        ? string.Empty
        : Path.GetFileName(Path.TrimEndingDirectorySeparator(RepositoryPath));

    public bool HasRepository => !string.IsNullOrWhiteSpace(RepositoryPath);

    public string CurrentBranch
    {
        get => _currentBranch;
        private set
        {
            if (Set(ref _currentBranch, value)) OnPropertyChanged(nameof(BranchToolTip));
        }
    }

    public string BranchToolTip => string.IsNullOrWhiteSpace(CurrentBranch)
        ? Text["NoBranch"]
        : $"{Text["Branch"]}: {CurrentBranch}";

    public string PullToolTip => Text["Pull"];

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
            _codexStatusKind == CodexCliStatusKindModel.Ready ? "Active" : "Warning"];

    public string CodexStatusText => _codexStatusKind == CodexCliStatusKindModel.Ready
        ? SelectedLanguage == "Deutsch" ? "ChatGPT verbunden" : "ChatGPT connected"
        : SelectedLanguage == "Deutsch"
            ? "ChatGPT nicht verbunden"
            : "ChatGPT not connected";

    public string CodexAccountText => _codexStatusKind == CodexCliStatusKindModel.Ready
        ? _codexEmail ?? (SelectedLanguage == "Deutsch" ? "Angemeldet" : "Signed in")
        : SelectedLanguage == "Deutsch"
            ? "Anmelden"
            : "Sign in";

    public string WeeklyQuotaText => _weeklyRemainingPercent is int remaining ? $"{remaining} %" : "–";

    public string WeeklyQuotaToolTip => _weeklyRemainingPercent is int remaining
        ? SelectedLanguage == "Deutsch"
            ? $"Wöchentliches Restkontingent: {remaining} %"
            : $"Weekly remaining quota: {remaining}%"
        : SelectedLanguage == "Deutsch"
            ? "Wöchentliches Restkontingent nicht verfügbar"
            : "Weekly remaining quota unavailable";

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

    public string AiDisclosureText { get; private set; } = "AI analysis has not been prepared.";

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
                OnPropertyChanged(nameof(SelectedReviewStatusText));
                OnPropertyChanged(nameof(AiDisclosureText));
                OnPropertyChanged(nameof(CodexStatusText));
                OnPropertyChanged(nameof(CodexAccountText));
                OnPropertyChanged(nameof(WeeklyQuotaText));
                OnPropertyChanged(nameof(WeeklyQuotaToolTip));
                OnPropertyChanged(nameof(BranchToolTip));
                OnPropertyChanged(nameof(PullToolTip));
                SavePreferences();
                _ = LoadAnalysisAsync(false);
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
                OnPropertyChanged(nameof(IsInter));
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

    public bool IsInter
    {
        get => SelectedFont == "Inter";
        set
        {
            if (value) SelectedFont = "Inter";
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

    public CommitHistoryItemModel? SelectedCommit
    {
        get => _selectedCommit;
        set
        {
            if (Set(ref _selectedCommit, value))
            {
                OnPropertyChanged(nameof(ViewTitle));
                _ = LoadAnalysisAsync(false);
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
                OnPropertyChanged(nameof(HasSelectedReview));
                OnPropertyChanged(nameof(SelectedReviewStatusText));
                OnPropertyChanged(nameof(SelectedReviewHint));
                OnPropertyChanged(nameof(HasSelectedReviewHint));
                OnPropertyChanged(nameof(IsSelectedReviewGood));
                OnPropertyChanged(nameof(IsSelectedReviewOk));
                OnPropertyChanged(nameof(IsSelectedReviewWarning));
            }
        }
    }

    public string SelectedAnalysisTitle => SelectedFile?.GroupTitle ?? Groups.FirstOrDefault()?.Title ?? string.Empty;

    public string SelectedAnalysisDescription =>
        SelectedFile?.GroupDescription ?? Groups.FirstOrDefault()?.Description ?? string.Empty;

    public bool HasSelectedReview => SelectedFile?.Review is not null;

    public string SelectedReviewStatusText => SelectedFile?.Review?.Status switch
    {
        DiffGroupReviewStatusModel.Good => SelectedLanguage == "Deutsch" ? "Review: Gut" : "Review: Good",
        DiffGroupReviewStatusModel.Ok => SelectedLanguage == "Deutsch" ? "Review: OK" : "Review: OK",
        DiffGroupReviewStatusModel.Warning => SelectedLanguage == "Deutsch" ? "Review: Warnung" : "Review: Warning",
        _ => string.Empty
    };

    public string SelectedReviewHint => SelectedFile?.Review?.Hint ?? string.Empty;
    public bool HasSelectedReviewHint => !string.IsNullOrWhiteSpace(SelectedReviewHint);
    public bool IsSelectedReviewGood => SelectedFile?.Review?.Status == DiffGroupReviewStatusModel.Good;
    public bool IsSelectedReviewOk => SelectedFile?.Review?.Status == DiffGroupReviewStatusModel.Ok;
    public bool IsSelectedReviewWarning => SelectedFile?.Review?.Status == DiffGroupReviewStatusModel.Warning;

    public RecentRepositoryItemModel? SelectedRecent
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
        _selectedFont = saved.Font is "Cascadia Code" or "Inter" or "Verdana" ? saved.Font : "Cascadia Code";
        _aiRepositoryApprovals = saved.Approvals.ToList();
        _aiRiskNoticeAccepted = saved.AiRiskNoticeAccepted;
        _lastShownReleaseNotesVersion = saved.LastShownReleaseNotesVersion;
        ThemeService.Apply(_selectedTheme);
        ApplyFont(_selectedFont);
        RecentRepositories.ReplaceAll(saved.RecentRepositories.Select(item => new RecentRepositoryItemModel(item)));
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(SelectedTheme));
        OnPropertyChanged(nameof(SelectedFont));
        if (!_aiRiskNoticeAccepted && !await ShowAiRiskNoticeAsync())
        {
            System.Windows.Application.Current.Shutdown();
            return;
        }

        await ShowReleaseNotesIfNeededAsync();
        await Task.Delay(750);
        await CheckCodexAsync();
    }

    public Task CheckForUpdatesOnStartupAsync()
    {
        return CheckForUpdatesAsync(false);
    }

    #region Private

    private Task CheckForUpdatesManuallyAsync()
    {
        return CheckForUpdatesAsync(true);
    }

    private async Task CheckForUpdatesAsync(bool showNoUpdateMessage)
    {
        var result = await _updates.CheckForUpdateAsync();
        switch (result.Status)
        {
            case ApplicationUpdateStatusModel.NoUpdate when showNoUpdateMessage:
                _dialogs.ShowInformation(UpdateTitle,
                    UpdateMessage("GrimmDiff is up to date.", "GrimmDiff ist aktuell."));
                break;
            case ApplicationUpdateStatusModel.NotInstalled when showNoUpdateMessage:
                _dialogs.ShowInformation(UpdateTitle, UpdateMessage(
                    "Updates are available after installing GrimmDiff with Setup.exe.",
                    "Updates stehen nach der Installation von GrimmDiff über Setup.exe zur Verfügung."));
                break;
            case ApplicationUpdateStatusModel.Failed when showNoUpdateMessage:
                _dialogs.ShowInformation(UpdateTitle, UpdateMessage(
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
        var message = UpdateMessage(
            $"Version {update.Version} is available. Download, install, and restart GrimmDiff now?",
            $"Version {update.Version} ist verfügbar. GrimmDiff jetzt herunterladen, installieren und neu starten?");
        if (!_dialogs.Confirm(UpdateTitle, message)) return;

        var applyResult = await _updates.DownloadAndRestartAsync();
        if (applyResult == ApplicationUpdateApplyResultModel.Failed)
            _dialogs.ShowInformation(UpdateTitle, UpdateMessage(
                "The update could not be installed. Please try again later.",
                "Das Update konnte nicht installiert werden. Bitte versuche es später erneut."));
    }

    private string UpdateTitle => SelectedLanguage == "Deutsch" ? "GrimmDiff-Update" : "GrimmDiff update";

    private string UpdateMessage(string english, string german)
    {
        return SelectedLanguage == "Deutsch" ? german : english;
    }

    private async Task ShowReleaseNotesIfNeededAsync()
    {
        if (!_updates.IsInstalled) return;
        var notes = ReleaseNotesReaderService.Read(Path.Combine(AppContext.BaseDirectory, "ReleaseNotes.md"));
        var version = typeof(MainViewModel).Assembly.GetName().Version?.ToString(3);
        if (notes is null || string.IsNullOrWhiteSpace(version) || notes.Version != version ||
            notes.Version == _lastShownReleaseNotesVersion) return;

        var title = UpdateMessage($"What's new in version {notes.Version}", $"Neu in Version {notes.Version}");
        _dialogs.ShowReleaseNotes(title, notes.ForLanguage(SelectedLanguage));
        _lastShownReleaseNotesVersion = notes.Version;
        await SavePreferencesAsync();
    }

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
        Branches.Clear();
        CurrentBranch = string.Empty;
        RevokeAiApprovalCommand.RaiseCanExecuteChanged();
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
        if (SelectedCommit is null) await LoadAnalysisAsync(false);
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
            History.ReplaceAll(snapshot.History.Select(commit => new CommitHistoryItemModel(commit)));
            Branches.ReplaceAll(snapshot.Branches);
            CurrentBranch = snapshot.Branch;
            SwitchBranchCommand.RaiseCanExecuteChanged();
            StatusMessage = snapshot.State == RepositoryStateModel.Ready
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

    private bool CanRunRepositoryOperation()
    {
        return !string.IsNullOrWhiteSpace(RepositoryPath) && !_isRepositoryOperation;
    }

    private async Task PullAsync()
    {
        await RunRepositoryOperationAsync(() => _workspace.PullAsync(RepositoryPath),
            SelectedLanguage == "Deutsch" ? "Änderungen werden geholt..." : "Pulling changes...");
    }

    private async Task SwitchBranchAsync(BranchInfoModel branch)
    {
        if (branch.IsCurrent) return;
        await RunRepositoryOperationAsync(() => _workspace.SwitchBranchAsync(RepositoryPath, branch.Name),
            SelectedLanguage == "Deutsch" ? $"Wechsle zu {branch.Name}..." : $"Switching to {branch.Name}...");
    }

    private async Task RunRepositoryOperationAsync(Func<Task<GitCommandResultModel>> operation, string progressMessage)
    {
        if (!CanRunRepositoryOperation()) return;
        _isRepositoryOperation = true;
        PullCommand.RaiseCanExecuteChanged();
        SwitchBranchCommand.RaiseCanExecuteChanged();
        try
        {
            StatusMessage = progressMessage;
            var result = await operation();
            if (!result.IsSuccess)
            {
                StatusMessage = result.ErrorMessage;
                return;
            }

            SelectedCommit = null;
            await RefreshAsync(false);
        }
        finally
        {
            _isRepositoryOperation = false;
            PullCommand.RaiseCanExecuteChanged();
            SwitchBranchCommand.RaiseCanExecuteChanged();
        }
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
                await _analysis.AnalyzeAsync(new DiffAnalysisQueryModel(repositoryPath, commitId, language, analyzeWithAi),
                    cancellationToken);
            // Grouping can concatenate large diffs. Keep that allocation off the dispatcher.
            var prepared = (Analysis: analysisView.Result,
                Groups: await Task.Run(
                    () => analysisView.Result.Groups.Select(group => new AnalysisGroupViewModel(group)).ToList(),
                    cancellationToken));
            if (generation != Interlocked.Read(ref _analysisGeneration)) return;
            var result = prepared.Analysis;
            if (analyzeWithAi && result.AiPreview is not null) SetAiDisclosure(result.AiPreview);
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
            OnPropertyChanged(nameof(HasSelectedReview));
            OnPropertyChanged(nameof(SelectedReviewStatusText));
            OnPropertyChanged(nameof(SelectedReviewHint));
            OnPropertyChanged(nameof(HasSelectedReviewHint));
            OnPropertyChanged(nameof(IsSelectedReviewGood));
            OnPropertyChanged(nameof(IsSelectedReviewOk));
            OnPropertyChanged(nameof(IsSelectedReviewWarning));
            OnPropertyChanged(nameof(ViewTitle));
            IsAnalysisStale = !analyzeWithAi;
            AnalysisSourceLabel = result.Source == DiffAnalysisSourceModel.Codex
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
        var language = SelectedLanguage == "Deutsch" ? "de" : "en";
        var preview = await _analysis.PreviewAiAsync(
            new DiffAnalysisQueryModel(RepositoryPath, SelectedCommit?.Commit.Id, language, true));
        SetAiDisclosure(preview);
        var repositoryId = RepositoryIdentity(RepositoryPath);
        if (!_aiRepositoryApprovals.Any(approval => approval.RepositoryId == repositoryId &&
                                                    approval.ProviderId == "ChatGPT Codex CLI" &&
                                                    approval.SafetyPolicyVersion == AiDiffSafetyPolicy.Version))
        {
            var confirmation = _dialogs.Ask("AI analysis",
                $"This repository approval permits optional processing by the signed-in ChatGPT workspace. This analysis will transfer {preview.TransferableSections} changed section(s), up to {preview.TransferableCharacters:N0} characters. {preview.ExcludedSections} section(s) are excluded and stay local. Approval is invalidated when repository configuration, provider, or the safety policy changes. Type ALLOW to approve this repository.");
            if (!string.Equals(confirmation, "ALLOW", StringComparison.Ordinal))
            {
                StatusMessage = "AI analysis was not approved.";
                return;
            }

            _aiRepositoryApprovals.RemoveAll(approval =>
                approval.RepositoryId.StartsWith(Path.GetFullPath(RepositoryPath), StringComparison.OrdinalIgnoreCase));
            _aiRepositoryApprovals.Add(new AiRepositoryApprovalModel(repositoryId, "ChatGPT Codex CLI",
                AiDiffSafetyPolicy.Version, DateTimeOffset.UtcNow));
            SavePreferences();
        }

        await LoadAnalysisAsync(true);
    }

    private Task RevokeAiApprovalAsync()
    {
        var root = Path.GetFullPath(RepositoryPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        _aiRepositoryApprovals.RemoveAll(approval =>
            approval.RepositoryId.StartsWith(root + "|", StringComparison.OrdinalIgnoreCase));
        SavePreferences();
        StatusMessage = SelectedLanguage == "Deutsch"
            ? "Die KI-Freigabe für dieses Repository wurde widerrufen."
            : "AI approval for this repository was revoked.";
        return Task.CompletedTask;
    }

    private Task<bool> ShowAiRiskNoticeAsync()
    {
        var german = SelectedLanguage == "Deutsch";
        var confirmation = _dialogs.Ask(german ? "KI-Risikohinweis" : "AI risk notice", german
            ? "Die Anwendung hat Zugriff auf das von dir geöffnete lokale Git-Repository. Die optionale KI-Analyse benötigt eine separate Repository-Freigabe. Vor jeder Analyse zeigt die Anwendung Umfang und ausgeschlossene Abschnitte; sensible, binäre und zu große Änderungen bleiben lokal. Die KI wird im angemeldeten ChatGPT-Workspace ausgeführt; sie hat keinen freien Lesezugriff auf weitere Repository-Dateien.\n\nTippe ACCEPT, um die Anwendung zu verwenden."
            : "The application can access the local Git repository you open. Optional AI analysis requires separate repository approval. Before every analysis, the application shows the scope and excluded sections; sensitive, binary, and oversized changes stay local. AI runs in your signed-in ChatGPT workspace and has no unrestricted access to other repository files.\n\nType ACCEPT to use the application.");
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
            SetCodexStatus(new CodexCliStatusModel(CodexCliStatusKindModel.Unavailable, string.Empty));
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
            SetCodexStatus(new CodexCliStatusModel(CodexCliStatusKindModel.Unavailable, string.Empty));
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
            OnPropertyChanged(nameof(WeeklyQuotaToolTip));
        }
        finally
        {
            _accountRefreshGate.Release();
        }
    }

    private void SetCodexStatus(CodexCliStatusModel status)
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
        OnPropertyChanged(nameof(WeeklyQuotaToolTip));
    }

    private string LocalizeCodexStatus(CodexCliStatusModel status)
    {
        return status.Kind switch
        {
            CodexCliStatusKindModel.Ready => SelectedLanguage == "Deutsch"
                ? "Codex ist mit ChatGPT angemeldet."
                : "Codex is signed in with ChatGPT.",
            CodexCliStatusKindModel.Missing => SelectedLanguage == "Deutsch"
                ? "Codex CLI wurde nicht gefunden."
                : "Codex CLI was not found.",
            CodexCliStatusKindModel.NotAuthenticated => SelectedLanguage == "Deutsch"
                ? "Codex ist nicht angemeldet."
                : "Codex is not signed in.",
            CodexCliStatusKindModel.ApiKeyAuthentication or CodexCliStatusKindModel.UnsupportedAuthentication =>
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
                () => DiffRenderer.CreatePage(file.Hunks, cancellationToken: cancellationToken),
                cancellationToken);
            if (generation != Interlocked.Read(ref _diffRenderGeneration) ||
                !ReferenceEquals(file, SelectedFile)) return;
            file.RenderDocument = DiffRenderDocumentModel.FromPage(page);
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
                () => DiffRenderer.CreatePage(file.Hunks, document.NextPage, cancellationToken),
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

    private void PublishDiffDocument(DiffRenderDocumentModel? document)
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

    private void SetAiDisclosure(AiAnalysisPreviewModel preview)
    {
        AiDisclosureText = SelectedLanguage == "Deutsch"
            ? $"KI-Datenübersicht: {preview.TransferableSections} von {preview.TotalSections} Abschnitten ({preview.TransferableCharacters:N0} Zeichen) werden an ChatGPT übertragen; {preview.ExcludedSections} bleiben lokal."
            : $"AI data overview: {preview.TransferableSections} of {preview.TotalSections} sections ({preview.TransferableCharacters:N0} characters) are sent to ChatGPT; {preview.ExcludedSections} stay local.";
        OnPropertyChanged(nameof(AiDisclosureText));
    }

    private void AddRecent(string path)
    {
        var preferences = _preferences.AddRecent(CurrentPreferences(), path);
        RecentRepositories.ReplaceAll(preferences.RecentRepositories.Select(item => new RecentRepositoryItemModel(item)));
        SavePreferences();
    }

    private void SavePreferences()
    {
        _ = SavePreferencesAsync();
    }

    private Task SavePreferencesAsync()
    {
        return _preferences.SaveAsync(CurrentPreferences());
    }

    private AppPreferencesModel CurrentPreferences()
    {
        return new AppPreferencesModel(RecentRepositories.Select(item => item.Path).ToList(),
            SelectedTheme, SelectedLanguage, SelectedFont, _aiRepositoryApprovals, _aiRiskNoticeAccepted,
            _lastShownReleaseNotesVersion);
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
