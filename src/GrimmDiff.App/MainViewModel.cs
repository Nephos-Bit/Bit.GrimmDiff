using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using GrimmDiff.Application;
using GrimmDiff.Domain;

namespace GrimmDiff.App;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly RepositoryWorkspaceService _workspace; private readonly DiffAnalysisService _analysis; private readonly CodexSessionService _codex; private readonly PreferencesService _preferences; private readonly IUserDialogService _dialogs; private readonly IExternalNavigationService _navigation; private readonly IRepositoryChangeMonitor _changeMonitor;
    private readonly System.Timers.Timer _refreshTimer = new(500) { AutoReset = false };
    private string _repositoryPath = string.Empty, _statusMessage = "Select a repository to inspect its changes.", _selectedLanguage = "English", _selectedTheme = "System", _selectedFont = "Cascadia Code";
    private string _analysisSourceLabel = "No analysis yet.", _aiStatusMessage = "Checking Codex CLI...";
    private string? _codexEmail; private int? _weeklyRemainingPercent;
    private CommitHistoryItem? _selectedCommit; private AnalysisFileViewModel? _selectedFile; private string? _selectedRecent; private bool _loading, _isAnalyzing, _isAnalysisStale;
    private CodexCliStatusKind _codexStatusKind = CodexCliStatusKind.Unavailable;
    private List<AiRepositoryApproval> _aiRepositoryApprovals = [];
    private bool _aiRiskNoticeAccepted;
    private CancellationTokenSource? _analysisCancellation; private long _analysisGeneration; private long _diffRenderGeneration; private int _initialized;
    public MainViewModel(RepositoryWorkspaceService workspace, DiffAnalysisService analysis, CodexSessionService codex, PreferencesService preferences, IUserDialogService dialogs, IExternalNavigationService navigation, IRepositoryChangeMonitor changeMonitor)
    {
        _workspace = workspace; _analysis = analysis; _codex = codex; _preferences = preferences; _dialogs = dialogs; _navigation = navigation; _changeMonitor = changeMonitor; _refreshTimer.Elapsed += OnRefreshTimerElapsed;
        OpenRepositoryCommand = new AsyncCommand(OpenRepositoryAsync); CloneCommand = new AsyncCommand(CloneAsync); ShowCurrentCommand = new AsyncCommand(ShowCurrentAsync); SelectFileCommand = new DelegateCommand<AnalysisFileViewModel>(SelectFile); OpenUrlCommand = new DelegateCommand<string>(OpenUrl);
        AnalyzeWithAiCommand = new AsyncCommand(AnalyzeWithAiAsync, () => !string.IsNullOrWhiteSpace(RepositoryPath) && !IsAnalyzing);
        LoginToCodexCommand = new AsyncCommand(LoginToCodexAsync, () => !IsAnalyzing);
        CancelAnalysisCommand = new AsyncCommand(CancelAnalysisAsync, () => IsAnalyzing);
        ShowAiRiskNoticeCommand = new AsyncCommand(async () => _ = await ShowAiRiskNoticeAsync());
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public BulkObservableCollection<RecentRepositoryItem> RecentRepositories { get; } = []; public BulkObservableCollection<CommitHistoryItem> History { get; } = []; public BulkObservableCollection<AnalysisGroupViewModel> Groups { get; } = [];
    private IReadOnlyList<DiffLineViewModel> _diffLines = []; public IReadOnlyList<DiffLineViewModel> DiffLines { get => _diffLines; private set => Set(ref _diffLines, value); }
    public IReadOnlyList<string> Languages { get; } = ["English", "Deutsch"]; public IReadOnlyList<string> Themes { get; } = ["System", "Dark", "Light"]; public IReadOnlyList<string> Fonts { get; } = ["Cascadia Code", "Verdana"]; public LocalizedText Text => new(() => SelectedLanguage);
    public AsyncCommand OpenRepositoryCommand { get; } public AsyncCommand CloneCommand { get; } public AsyncCommand ShowCurrentCommand { get; } public AsyncCommand AnalyzeWithAiCommand { get; } public AsyncCommand LoginToCodexCommand { get; } public AsyncCommand CancelAnalysisCommand { get; } public AsyncCommand ShowAiRiskNoticeCommand { get; } public DelegateCommand<AnalysisFileViewModel> SelectFileCommand { get; } public DelegateCommand<string> OpenUrlCommand { get; }
    public string RepositoryPath { get => _repositoryPath; private set { if (Set(ref _repositoryPath, value)) { OnPropertyChanged(nameof(RepositoryName)); AnalyzeWithAiCommand.RaiseCanExecuteChanged(); } } } public string RepositoryName => string.IsNullOrWhiteSpace(RepositoryPath) ? string.Empty : Path.GetFileName(Path.TrimEndingDirectorySeparator(RepositoryPath)); public string StatusMessage { get => _statusMessage; private set => Set(ref _statusMessage, value); }
    public string AnalysisSourceLabel { get => _analysisSourceLabel; private set => Set(ref _analysisSourceLabel, value); }
    public string AiStatusMessage { get => _aiStatusMessage; private set => Set(ref _aiStatusMessage, value); }
    public Brush CodexStatusBrush => (Brush)System.Windows.Application.Current.Resources[_codexStatusKind == CodexCliStatusKind.Ready ? "Active" : "Warning"];
    public string CodexStatusText => _codexStatusKind == CodexCliStatusKind.Ready ? (SelectedLanguage == "Deutsch" ? "ChatGPT verbunden" : "ChatGPT connected") : (SelectedLanguage == "Deutsch" ? "ChatGPT nicht verbunden" : "ChatGPT not connected");
    public string CodexAccountText => _codexStatusKind == CodexCliStatusKind.Ready
        ? _codexEmail ?? (SelectedLanguage == "Deutsch" ? "Angemeldet" : "Signed in")
        : SelectedLanguage == "Deutsch" ? "Anmelden" : "Sign in";
    public string WeeklyQuotaText => _weeklyRemainingPercent is int remaining
        ? SelectedLanguage == "Deutsch" ? $"WOCHE · {remaining} % VERBLEIBEND" : $"WEEK · {remaining}% LEFT"
        : SelectedLanguage == "Deutsch" ? "WOCHE · –" : "WEEK · –";
    public bool IsAnalyzing { get => _isAnalyzing; private set { if (Set(ref _isAnalyzing, value)) { AnalyzeWithAiCommand.RaiseCanExecuteChanged(); LoginToCodexCommand.RaiseCanExecuteChanged(); CancelAnalysisCommand.RaiseCanExecuteChanged(); } } }
    public bool IsAnalysisStale { get => _isAnalysisStale; private set => Set(ref _isAnalysisStale, value); }
    public string AnalysisProgressText => SelectedLanguage == "Deutsch" ? "GPT-6 Luna analysiert die Änderungen..." : "GPT-6 Luna is analysing the changes...";
    public string AnalyzeButtonText => SelectedLanguage == "Deutsch" ? "Mit GPT-6 Luna analysieren" : "Analyse with GPT-6 Luna";
    public string LoginButtonText => SelectedLanguage == "Deutsch" ? "Mit ChatGPT anmelden" : "Sign in with ChatGPT";
    public string CancelButtonText => SelectedLanguage == "Deutsch" ? "Abbrechen" : "Cancel";
    public string AiDisclosureText => SelectedLanguage == "Deutsch" ? "Die CLI läuft lokal; gelesene Repository-Inhalte werden über den angemeldeten ChatGPT-Workspace verarbeitet." : "The CLI runs locally; repository content it reads is processed through the signed-in ChatGPT workspace.";
    public string SelectedLanguage { get => _selectedLanguage; set { if (Set(ref _selectedLanguage, value)) { OnPropertyChanged(nameof(Text)); OnPropertyChanged(nameof(IsEnglish)); OnPropertyChanged(nameof(IsGerman)); OnPropertyChanged(nameof(AnalyzeButtonText)); OnPropertyChanged(nameof(LoginButtonText)); OnPropertyChanged(nameof(CancelButtonText)); OnPropertyChanged(nameof(AnalysisProgressText)); OnPropertyChanged(nameof(AiDisclosureText)); OnPropertyChanged(nameof(CodexStatusText)); OnPropertyChanged(nameof(CodexAccountText)); OnPropertyChanged(nameof(WeeklyQuotaText)); SavePreferences(); _ = LoadAnalysisAsync(true); } } }
    public string SelectedTheme { get => _selectedTheme; set { if (Set(ref _selectedTheme, value)) { OnPropertyChanged(nameof(IsSystemTheme)); OnPropertyChanged(nameof(IsDarkTheme)); OnPropertyChanged(nameof(IsLightTheme)); ThemeService.Apply(value); OnPropertyChanged(nameof(CodexStatusBrush)); _ = ShowFileAsync(SelectedFile); SavePreferences(); } } }
    public string SelectedFont { get => _selectedFont; set { if (Set(ref _selectedFont, value)) { OnPropertyChanged(nameof(IsCascadiaCode)); OnPropertyChanged(nameof(IsVerdana)); ApplyFont(value); SavePreferences(); } } }
    public bool IsEnglish { get => SelectedLanguage == "English"; set { if (value) SelectedLanguage = "English"; } }
    public bool IsGerman { get => SelectedLanguage == "Deutsch"; set { if (value) SelectedLanguage = "Deutsch"; } }
    public bool IsSystemTheme { get => SelectedTheme == "System"; set { if (value) SelectedTheme = "System"; } }
    public bool IsDarkTheme { get => SelectedTheme == "Dark"; set { if (value) SelectedTheme = "Dark"; } }
    public bool IsLightTheme { get => SelectedTheme == "Light"; set { if (value) SelectedTheme = "Light"; } }
    public bool IsCascadiaCode { get => SelectedFont == "Cascadia Code"; set { if (value) SelectedFont = "Cascadia Code"; } }
    public bool IsVerdana { get => SelectedFont == "Verdana"; set { if (value) SelectedFont = "Verdana"; } }
    public CommitHistoryItem? SelectedCommit { get => _selectedCommit; set { if (Set(ref _selectedCommit, value)) { OnPropertyChanged(nameof(ViewTitle)); _ = LoadAnalysisAsync(true); } } }
    public AnalysisFileViewModel? SelectedFile { get => _selectedFile; set { if (Set(ref _selectedFile, value)) { _ = ShowFileAsync(value); OnPropertyChanged(nameof(SelectedAnalysisTitle)); OnPropertyChanged(nameof(SelectedAnalysisDescription)); } } }
    public string SelectedAnalysisTitle => SelectedFile?.GroupTitle ?? Groups.FirstOrDefault()?.Title ?? string.Empty;
    public string SelectedAnalysisDescription => SelectedFile?.GroupDescription ?? Groups.FirstOrDefault()?.Description ?? string.Empty;
    public RecentRepositoryItem? SelectedRecent { get => RecentRepositories.FirstOrDefault(item => item.Path == _selectedRecent); set { if (Set(ref _selectedRecent, value?.Path) && value is not null) _ = OpenRepositoryAsync(value.Path); } }
    public string ViewTitle => SelectedCommit is null ? "Current changes" : $"{SelectedCommit.Commit.Subject} · {SelectedCommit.Commit.Id[..Math.Min(8, SelectedCommit.Commit.Id.Length)]}";
    public async Task InitializeAsync()
    {
        if (Interlocked.Exchange(ref _initialized, 1) != 0) return;
        var saved = await _preferences.LoadAsync(); _selectedLanguage = saved.Language; _selectedTheme = saved.Theme; _selectedFont = saved.Font is "Cascadia Code" or "Verdana" ? saved.Font : "Cascadia Code"; _aiRepositoryApprovals = saved.Approvals.ToList(); _aiRiskNoticeAccepted = saved.AiRiskNoticeAccepted; ThemeService.Apply(_selectedTheme); ApplyFont(_selectedFont);
        RecentRepositories.ReplaceAll(saved.RecentRepositories.Select(item => new RecentRepositoryItem(item)));
        OnPropertyChanged(nameof(SelectedLanguage)); OnPropertyChanged(nameof(SelectedTheme)); OnPropertyChanged(nameof(SelectedFont));
        if (!_aiRiskNoticeAccepted && !await ShowAiRiskNoticeAsync()) { System.Windows.Application.Current.Shutdown(); return; }
        await Task.Delay(750);
        await CheckCodexAsync();
    }

    public void Dispose()
    {
        _analysisCancellation?.Cancel();
        _analysisCancellation?.Dispose();
        _changeMonitor.Dispose();
        _refreshTimer.Dispose();
    }

    #region Private

    private async Task OpenRepositoryAsync() { var path = _dialogs.SelectRepository(); if (!string.IsNullOrWhiteSpace(path)) await OpenRepositoryAsync(path); }
    private async Task OpenRepositoryAsync(string path) { if (!await _workspace.IsGitAvailableAsync()) { StatusMessage = "Git is not installed or unavailable on PATH."; return; } RepositoryPath = path; AddRecent(path); _changeMonitor.Watch(path, ScheduleRefresh); await RefreshAsync(false); }
    private async Task CloneAsync() { var source = _dialogs.Ask("Clone repository", "Repository URL or local source:"); if (string.IsNullOrWhiteSpace(source)) return; var destination = _dialogs.Ask("Clone repository", "Destination folder:"); if (string.IsNullOrWhiteSpace(destination)) return; var result = await _workspace.CloneAsync(source, destination); StatusMessage = result.IsSuccess ? "Repository cloned." : result.ErrorMessage; if (result.IsSuccess) await OpenRepositoryAsync(destination); }
    private async Task ShowCurrentAsync() { if (SelectedCommit is null) await LoadAnalysisAsync(true); else SelectedCommit = null; }
    private async Task RefreshAsync(bool analyzeWithAi)
    {
        if (string.IsNullOrWhiteSpace(RepositoryPath) || _loading) return; _loading = true;
        var refreshAnalysis = false;
        try { var repositoryPath = RepositoryPath; var snapshot = await _workspace.OpenAsync(repositoryPath); History.ReplaceAll(snapshot.History.Select(commit => new CommitHistoryItem(commit))); StatusMessage = snapshot.State == RepositoryState.Ready ? $"{snapshot.Changes.Count} current file changes." : $"Git state: {snapshot.State}. Resolve it before editing."; refreshAnalysis = SelectedCommit is null || analyzeWithAi; }
        catch (Exception ex) { StatusMessage = ex.Message; }
        finally { _loading = false; }
        if (refreshAnalysis) await LoadAnalysisAsync(analyzeWithAi);
    }
    private async Task LoadAnalysisAsync(bool analyzeWithAi)
    {
        if (string.IsNullOrWhiteSpace(RepositoryPath)) return;
        var generation = Interlocked.Increment(ref _analysisGeneration);
        _analysisCancellation?.Cancel(); _analysisCancellation?.Dispose(); _analysisCancellation = new CancellationTokenSource();
        var cancellationToken = _analysisCancellation.Token;
        var repositoryPath = RepositoryPath;
        var commitId = SelectedCommit?.Commit.Id;
        var language = SelectedLanguage == "Deutsch" ? "de" : "en";
        IsAnalyzing = analyzeWithAi;
        if (analyzeWithAi) StatusMessage = AnalysisProgressText;
        try
        {
            var analysisView = await _analysis.AnalyzeAsync(new DiffAnalysisQuery(repositoryPath, commitId, language, analyzeWithAi), cancellationToken);
            // Grouping can concatenate large diffs. Keep that allocation off the dispatcher.
            var prepared = (Analysis: analysisView.Result, Groups: await Task.Run(() => analysisView.Result.Groups.Select(group => new AnalysisGroupViewModel(group)).ToList(), cancellationToken));
            if (generation != Interlocked.Read(ref _analysisGeneration)) return;
            var result = prepared.Analysis;
            Groups.ReplaceAll(prepared.Groups);
            var firstFile = Groups.SelectMany(group => group.Files).FirstOrDefault();
            if (!Equals(SelectedFile, firstFile)) SelectedFile = firstFile; else _ = ShowFileAsync(firstFile);
            OnPropertyChanged(nameof(SelectedAnalysisTitle)); OnPropertyChanged(nameof(SelectedAnalysisDescription)); OnPropertyChanged(nameof(ViewTitle));
            IsAnalysisStale = !analyzeWithAi;
            AnalysisSourceLabel = result.Source == DiffAnalysisSource.Codex
                ? (SelectedLanguage == "Deutsch" ? "GPT-6 Luna · KI-Gruppierung" : "GPT-6 Luna · AI grouping")
                : analyzeWithAi
                    ? (SelectedLanguage == "Deutsch" ? "Regelbasierter Fallback" : "Rule-based fallback")
                    : (SelectedLanguage == "Deutsch" ? "Regelbasierte Vorschau · KI-Neuanalyse verfügbar" : "Rule-based preview · AI refresh available");
            var summary = result.Hunks.Count == 0
                ? (SelectedLanguage == "Deutsch" ? "Keine analysierbaren Änderungen." : "No textual changes to analyse.")
                : SelectedLanguage == "Deutsch"
                    ? $"{result.Hunks.Count} geänderte Abschnitte wurden in {result.Groups.Count} Arbeitsschritte gruppiert."
                    : $"{result.Hunks.Count} changed sections grouped into {result.Groups.Count} work steps.";
            StatusMessage = string.IsNullOrWhiteSpace(result.Diagnostic) ? summary : $"{result.Diagnostic} {summary}";
            if (result.Source == DiffAnalysisSource.Codex) _ = RefreshCodexAccountAsync();
        }
        catch (OperationCanceledException) { if (generation == Interlocked.Read(ref _analysisGeneration)) StatusMessage = SelectedLanguage == "Deutsch" ? "Analyse abgebrochen." : "Analysis cancelled."; }
        catch (Exception ex) { StatusMessage = ex.Message; }
        finally { if (generation == Interlocked.Read(ref _analysisGeneration)) { IsAnalyzing = false; _analysisCancellation?.Dispose(); _analysisCancellation = null; } }
    }
    private async Task AnalyzeWithAiAsync()
    {
        var repositoryId = RepositoryIdentity(RepositoryPath);
        if (!_aiRepositoryApprovals.Any(approval => approval.RepositoryId == repositoryId))
        {
            var confirmation = _dialogs.Ask("AI analysis", "Repository diffs, including untracked files except protected key and credential files, will be processed by your signed-in ChatGPT workspace. Type ALLOW to approve this repository.");
            if (!string.Equals(confirmation, "ALLOW", StringComparison.Ordinal)) { StatusMessage = "AI analysis was not approved."; return; }
            _aiRepositoryApprovals.RemoveAll(approval => approval.RepositoryId.StartsWith(Path.GetFullPath(RepositoryPath), StringComparison.OrdinalIgnoreCase));
            _aiRepositoryApprovals.Add(new AiRepositoryApproval(repositoryId, "Pending trusted CLI", "Pending verification", DateTimeOffset.UtcNow));
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
        if (!_aiRiskNoticeAccepted) { _aiRiskNoticeAccepted = true; SavePreferences(); }
        return Task.FromResult(true);
    }
    private Task CancelAnalysisAsync() { _analysisCancellation?.Cancel(); return Task.CompletedTask; }
    private async Task CheckCodexAsync() { try { var status = await Task.Run(() => _codex.GetStatusAsync()); SetCodexStatus(status); } catch (Exception) { SetCodexStatus(new CodexCliStatus(CodexCliStatusKind.Unavailable, string.Empty)); } }
    private async Task LoginToCodexAsync() { AiStatusMessage = SelectedLanguage == "Deutsch" ? "ChatGPT-Anmeldung wird geöffnet..." : "Opening ChatGPT sign-in..."; try { var status = await Task.Run(() => _codex.LoginAsync()); SetCodexStatus(status); if (status.IsReady) await RefreshCodexAccountAsync(); } catch (Exception) { SetCodexStatus(new CodexCliStatus(CodexCliStatusKind.Unavailable, string.Empty)); } }
    private async Task RefreshCodexAccountAsync()
    {
        var usage = await Task.Run(() => _codex.GetAccountUsageAsync());
        _codexEmail = usage.Email;
        _weeklyRemainingPercent = usage.WeeklyRemainingPercent;
        OnPropertyChanged(nameof(CodexAccountText));
        OnPropertyChanged(nameof(WeeklyQuotaText));
    }
    private void SetCodexStatus(CodexCliStatus status) { _codexStatusKind = status.Kind; if (!status.IsReady) { _codexEmail = null; _weeklyRemainingPercent = null; } AiStatusMessage = LocalizeCodexStatus(status); OnPropertyChanged(nameof(CodexStatusBrush)); OnPropertyChanged(nameof(CodexStatusText)); OnPropertyChanged(nameof(CodexAccountText)); OnPropertyChanged(nameof(WeeklyQuotaText)); }
    private string LocalizeCodexStatus(CodexCliStatus status) => status.Kind switch
    {
        CodexCliStatusKind.Ready => SelectedLanguage == "Deutsch" ? "Codex ist mit ChatGPT angemeldet." : "Codex is signed in with ChatGPT.",
        CodexCliStatusKind.Missing => SelectedLanguage == "Deutsch" ? "Codex CLI wurde nicht gefunden." : "Codex CLI was not found.",
        CodexCliStatusKind.NotAuthenticated => SelectedLanguage == "Deutsch" ? "Codex ist nicht angemeldet." : "Codex is not signed in.",
        CodexCliStatusKind.ApiKeyAuthentication or CodexCliStatusKind.UnsupportedAuthentication => SelectedLanguage == "Deutsch" ? "Codex verwendet keine ChatGPT-Anmeldung." : "Codex is not using a ChatGPT sign-in.",
        _ => SelectedLanguage == "Deutsch" ? "Codex ist derzeit nicht verfügbar." : "Codex is currently unavailable."
    };
    private void SelectFile(AnalysisFileViewModel file) { SelectedFile = file; }
    private void OpenUrl(string url) => _navigation.Open(url);
    private async Task ShowFileAsync(AnalysisFileViewModel? file)
    {
        var generation = Interlocked.Increment(ref _diffRenderGeneration);
        if (file is null) { DiffLines = []; return; }
        var palette = DiffLineViewModel.CapturePalette();
        var diff = file.Diff;
        // Prepare a bounded display model on a worker before the ListBox is updated.
        var lines = await Task.Run(() => DiffLineViewModel.CreateDisplayLines(diff, palette));
        if (generation == Interlocked.Read(ref _diffRenderGeneration) && Equals(file, SelectedFile)) DiffLines = lines;
    }
    private void ScheduleRefresh(string path) { if (IsGeneratedPath(path)) return; _analysisCancellation?.Cancel(); _refreshTimer.Stop(); _refreshTimer.Start(); }
    private static bool IsGeneratedPath(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (segments.Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase) || segment.Equals("obj", StringComparison.OrdinalIgnoreCase) || segment.Equals("node_modules", StringComparison.OrdinalIgnoreCase) || segment.Equals(".vs", StringComparison.OrdinalIgnoreCase))) return true;
        var gitIndex = Array.FindIndex(segments, segment => segment.Equals(".git", StringComparison.OrdinalIgnoreCase));
        if (gitIndex < 0 || gitIndex + 1 >= segments.Length) return gitIndex >= 0;
        var gitEntry = segments[gitIndex + 1];
        return !gitEntry.Equals("HEAD", StringComparison.OrdinalIgnoreCase)
            && !gitEntry.Equals("refs", StringComparison.OrdinalIgnoreCase)
            && !gitEntry.Equals("packed-refs", StringComparison.OrdinalIgnoreCase);
    }
    private async void OnRefreshTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e) => await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => RefreshAsync(false)).Task.Unwrap();
    private static void ApplyFont(string font) => System.Windows.Application.Current.Resources["AppFontFamily"] = new FontFamily(font);
    private void AddRecent(string path) { var preferences = _preferences.AddRecent(new AppPreferences(RecentRepositories.Select(item => item.Path).ToList(), SelectedTheme, SelectedLanguage, SelectedFont), path); RecentRepositories.ReplaceAll(preferences.RecentRepositories.Select(item => new RecentRepositoryItem(item))); SavePreferences(); }
    private void SavePreferences() => _ = _preferences.SaveAsync(new AppPreferences(RecentRepositories.Select(item => item.Path).ToList(), SelectedTheme, SelectedLanguage, SelectedFont, _aiRepositoryApprovals, _aiRiskNoticeAccepted));
    private static string RepositoryIdentity(string repositoryPath)
    {
        var root = Path.GetFullPath(repositoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var config = Path.Combine(root, ".git", "config");
        var configHash = File.Exists(config) ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(config))) : "NO_CONFIG";
        return $"{root}|{configHash}";
    }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? property = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnPropertyChanged(property); return true; }
    private void OnPropertyChanged([CallerMemberName] string? property = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

    #endregion
}
public sealed class AnalysisGroupViewModel { public AnalysisGroupViewModel(WorkStepGroup group) { Title = group.Title; Description = group.Description; Files = group.Hunks.GroupBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase).Select(x => new AnalysisFileViewModel(x.Key, string.Join("\n", x.Select(h => $"{h.Header}\n{h.Content}")), group.Title, group.Description)).ToList(); } public string Title { get; } public string Description { get; } public IReadOnlyList<AnalysisFileViewModel> Files { get; } }
public sealed class AnalysisFileViewModel
{
    public AnalysisFileViewModel(string path, string diff, string groupTitle, string groupDescription) => (Path, Diff, GroupTitle, GroupDescription) = (path, diff, groupTitle, groupDescription);
    public string Path { get; }
    public string Diff { get; }
    public string GroupTitle { get; }
    public string GroupDescription { get; }

    // The generated record implementation includes Diff, which can be several MB.
    // WPF also hashes bound items, so this class intentionally uses reference identity.
    public override string ToString() => Path;
}
public sealed record RecentRepositoryItem(string Path)
{
    public string Name => System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(Path));
    public override string ToString() => Name;
}
public abstract record CommitSubjectPart(string Text);
public sealed record PlainCommitSubjectPart(string Text) : CommitSubjectPart(Text);
public sealed record JiraCommitSubjectPart(string Text, string Url) : CommitSubjectPart(Text);
public sealed class CommitHistoryItem
{
    private static readonly Regex JiraIssuePrefix = new(@"^\[(?<issue>[A-Za-z][A-Za-z0-9]*-\d+)\]", RegexOptions.Compiled);
    public CommitHistoryItem(CommitInfo commit)
    {
        Commit = commit;
        var match = JiraIssuePrefix.Match(commit.Subject);
        SubjectParts = match.Success
            ? [new JiraCommitSubjectPart(match.Value, $"https://jira/browse/{match.Groups["issue"].Value}"), new PlainCommitSubjectPart(commit.Subject[match.Length..])]
            : [new PlainCommitSubjectPart(commit.Subject)];
    }
    public CommitInfo Commit { get; }
    public IReadOnlyList<CommitSubjectPart> SubjectParts { get; }
}
public sealed record CodePalette(IReadOnlyDictionary<string, Brush> Brushes)
{
    public Brush this[string key] => Brushes[key];
}
public sealed class DiffLineViewModel
{
    private const int MaximumDisplayLines = 1_000;
    private const int MaximumDisplayLineLength = 600;
    private static readonly string[] PaletteKeys = ["Primary", "Secondary", "DiffAdded", "DiffAddedBackground", "DiffRemoved", "DiffRemovedBackground", "DiffHunk", "CodeKeyword", "CodeType", "CodeMember", "CodeString", "CodeComment"];
    public DiffLineViewModel(string line, CodePalette palette) => (Text, Foreground, Background) = Parse(line, palette);
    public string Text { get; }
    public Brush Foreground { get; }
    public Brush? Background { get; }
    public static CodePalette CapturePalette() => new(PaletteKeys.ToDictionary(key => key, key =>
    {
        var brush = ((Brush)System.Windows.Application.Current.Resources[key]).Clone();
        brush.Freeze();
        return brush;
    }, StringComparer.Ordinal));
    public static IReadOnlyList<DiffLineViewModel> CreateDisplayLines(string diff, CodePalette palette)
    {
        var sourceLines = diff.Split('\n');
        var count = Math.Min(sourceLines.Length, MaximumDisplayLines);
        var lines = new List<DiffLineViewModel>(count + (sourceLines.Length > count ? 1 : 0));
        for (var index = 0; index < count; index++)
        {
            var line = sourceLines[index];
            if (line.Length > MaximumDisplayLineLength) line = line[..MaximumDisplayLineLength] + " … [line truncated]";
            lines.Add(new DiffLineViewModel(line, palette));
        }
        if (sourceLines.Length > count) lines.Add(new DiffLineViewModel($"… [{sourceLines.Length - count:N0} further lines omitted for responsive display]", palette));
        return lines;
    }

    #region Private

    private static (string Text, Brush Foreground, Brush? Background) Parse(string line, CodePalette palette)
    {
        if (line.StartsWith('+') && !line.StartsWith("+++")) return (line, palette["DiffAdded"], palette["DiffAddedBackground"]);
        if (line.StartsWith('-') && !line.StartsWith("---")) return (line, palette["DiffRemoved"], palette["DiffRemovedBackground"]);
        if (line.StartsWith("@@")) return (line, palette["DiffHunk"], null);
        if (line.StartsWith("diff ") || line.StartsWith("+++") || line.StartsWith("---")) return (line, palette["Secondary"], null);
        return (line, palette["Primary"], null);
    }

    #endregion
}
public sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotifications;
    public void ReplaceAll(IEnumerable<T> values)
    {
        _suppressNotifications = true;
        try { ClearItems(); foreach (var value in values) Add(value); }
        finally { _suppressNotifications = false; }
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
    protected override void OnPropertyChanged(PropertyChangedEventArgs e) { if (!_suppressNotifications) base.OnPropertyChanged(e); }
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) { if (!_suppressNotifications) base.OnCollectionChanged(e); }
}
