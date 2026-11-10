using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using GrimmDiff.Domain;
using GrimmDiff.Infrastructure;

namespace GrimmDiff.App;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IGitRepositoryService _git; private readonly IDiffAnalysisProvider _analysis; private readonly IDiffAnalysisProvider _heuristicAnalysis; private readonly ICodexCliClient _codex; private readonly AppSettingsStore _settings = new();
    private readonly System.Timers.Timer _refreshTimer = new(500) { AutoReset = false }; private FileSystemWatcher? _watcher;
    private string _repositoryPath = string.Empty, _statusMessage = "Select a repository to inspect its changes.", _selectedLanguage = "English", _selectedTheme = "System", _selectedFont = "Cascadia Code";
    private string _analysisSourceLabel = "No analysis yet.", _aiStatusMessage = "Checking Codex CLI...";
    private string? _codexEmail; private int? _weeklyRemainingPercent;
    private CommitHistoryItem? _selectedCommit; private AnalysisFileViewModel? _selectedFile; private string? _selectedRecent; private bool _loading, _isAnalyzing, _isAnalysisStale;
    private CodexCliStatusKind _codexStatusKind = CodexCliStatusKind.Unavailable;
    private CancellationTokenSource? _analysisCancellation; private long _analysisGeneration;
    public MainViewModel(IGitRepositoryService git, IDiffAnalysisProvider analysis, IDiffAnalysisProvider heuristicAnalysis, ICodexCliClient codex)
    {
        _git = git; _analysis = analysis; _heuristicAnalysis = heuristicAnalysis; _codex = codex; _refreshTimer.Elapsed += OnRefreshTimerElapsed;
        var saved = _settings.Load(); _selectedLanguage = saved.Language; _selectedTheme = saved.Theme; _selectedFont = saved.Font is "Cascadia Code" or "Verdana" ? saved.Font : "Cascadia Code"; ThemeService.Apply(_selectedTheme); ApplyFont(_selectedFont);
        foreach (var item in saved.RecentRepositories.Where(Directory.Exists)) RecentRepositories.Add(new RecentRepositoryItem(item));
        OpenRepositoryCommand = new AsyncCommand(OpenRepositoryAsync); CloneCommand = new AsyncCommand(CloneAsync); ShowCurrentCommand = new AsyncCommand(ShowCurrentAsync); SelectFileCommand = new DelegateCommand<AnalysisFileViewModel>(SelectFile); OpenUrlCommand = new DelegateCommand<string>(OpenUrl);
        AnalyzeWithAiCommand = new AsyncCommand(AnalyzeWithAiAsync, () => !string.IsNullOrWhiteSpace(RepositoryPath) && !IsAnalyzing);
        LoginToCodexCommand = new AsyncCommand(LoginToCodexAsync, () => !IsAnalyzing);
        CancelAnalysisCommand = new AsyncCommand(CancelAnalysisAsync, () => IsAnalyzing);
        _ = CheckCodexAsync();
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<RecentRepositoryItem> RecentRepositories { get; } = []; public ObservableCollection<CommitHistoryItem> History { get; } = []; public ObservableCollection<AnalysisGroupViewModel> Groups { get; } = []; public ObservableCollection<DiffLineViewModel> DiffLines { get; } = [];
    public IReadOnlyList<string> Languages { get; } = ["English", "Deutsch"]; public IReadOnlyList<string> Themes { get; } = ["System", "Dark", "Light"]; public IReadOnlyList<string> Fonts { get; } = ["Cascadia Code", "Verdana"]; public LocalizedText Text => new(() => SelectedLanguage);
    public AsyncCommand OpenRepositoryCommand { get; } public AsyncCommand CloneCommand { get; } public AsyncCommand ShowCurrentCommand { get; } public AsyncCommand AnalyzeWithAiCommand { get; } public AsyncCommand LoginToCodexCommand { get; } public AsyncCommand CancelAnalysisCommand { get; } public DelegateCommand<AnalysisFileViewModel> SelectFileCommand { get; } public DelegateCommand<string> OpenUrlCommand { get; }
    public string RepositoryPath { get => _repositoryPath; private set { if (Set(ref _repositoryPath, value)) { OnPropertyChanged(nameof(RepositoryName)); AnalyzeWithAiCommand.RaiseCanExecuteChanged(); } } } public string RepositoryName => string.IsNullOrWhiteSpace(RepositoryPath) ? string.Empty : Path.GetFileName(Path.TrimEndingDirectorySeparator(RepositoryPath)); public string StatusMessage { get => _statusMessage; private set => Set(ref _statusMessage, value); }
    public string AnalysisSourceLabel { get => _analysisSourceLabel; private set => Set(ref _analysisSourceLabel, value); }
    public string AiStatusMessage { get => _aiStatusMessage; private set => Set(ref _aiStatusMessage, value); }
    public Brush CodexStatusBrush => (Brush)Application.Current.Resources[_codexStatusKind == CodexCliStatusKind.Ready ? "Active" : "Warning"];
    public string CodexStatusText => _codexStatusKind == CodexCliStatusKind.Ready ? (SelectedLanguage == "Deutsch" ? "ChatGPT verbunden" : "ChatGPT connected") : (SelectedLanguage == "Deutsch" ? "ChatGPT nicht verbunden" : "ChatGPT not connected");
    public string CodexAccountText => _codexStatusKind == CodexCliStatusKind.Ready
        ? _codexEmail ?? (SelectedLanguage == "Deutsch" ? "Angemeldet" : "Signed in")
        : SelectedLanguage == "Deutsch" ? "Anmelden" : "Sign in";
    public string WeeklyQuotaText => _weeklyRemainingPercent is int remaining
        ? SelectedLanguage == "Deutsch" ? $"WOCHE · {remaining} % VERBLEIBEND" : $"WEEK · {remaining}% LEFT"
        : SelectedLanguage == "Deutsch" ? "WOCHE · –" : "WEEK · –";
    public bool IsAnalyzing { get => _isAnalyzing; private set { if (Set(ref _isAnalyzing, value)) { AnalyzeWithAiCommand.RaiseCanExecuteChanged(); LoginToCodexCommand.RaiseCanExecuteChanged(); CancelAnalysisCommand.RaiseCanExecuteChanged(); } } }
    public bool IsAnalysisStale { get => _isAnalysisStale; private set => Set(ref _isAnalysisStale, value); }
    public string AnalyzeButtonText => SelectedLanguage == "Deutsch" ? "Mit GPT-6 Luna analysieren" : "Analyse with GPT-6 Luna";
    public string LoginButtonText => SelectedLanguage == "Deutsch" ? "Mit ChatGPT anmelden" : "Sign in with ChatGPT";
    public string CancelButtonText => SelectedLanguage == "Deutsch" ? "Abbrechen" : "Cancel";
    public string AiDisclosureText => SelectedLanguage == "Deutsch" ? "Die CLI läuft lokal; gelesene Repository-Inhalte werden über den angemeldeten ChatGPT-Workspace verarbeitet." : "The CLI runs locally; repository content it reads is processed through the signed-in ChatGPT workspace.";
    public string SelectedLanguage { get => _selectedLanguage; set { if (Set(ref _selectedLanguage, value)) { OnPropertyChanged(nameof(Text)); OnPropertyChanged(nameof(IsEnglish)); OnPropertyChanged(nameof(IsGerman)); OnPropertyChanged(nameof(AnalyzeButtonText)); OnPropertyChanged(nameof(LoginButtonText)); OnPropertyChanged(nameof(CancelButtonText)); OnPropertyChanged(nameof(AiDisclosureText)); OnPropertyChanged(nameof(CodexStatusText)); OnPropertyChanged(nameof(CodexAccountText)); OnPropertyChanged(nameof(WeeklyQuotaText)); SavePreferences(); _ = LoadAnalysisAsync(true); } } }
    public string SelectedTheme { get => _selectedTheme; set { if (Set(ref _selectedTheme, value)) { OnPropertyChanged(nameof(IsSystemTheme)); OnPropertyChanged(nameof(IsDarkTheme)); OnPropertyChanged(nameof(IsLightTheme)); ThemeService.Apply(value); OnPropertyChanged(nameof(CodexStatusBrush)); ShowFile(SelectedFile); SavePreferences(); } } }
    public string SelectedFont { get => _selectedFont; set { if (Set(ref _selectedFont, value)) { OnPropertyChanged(nameof(IsCascadiaCode)); OnPropertyChanged(nameof(IsVerdana)); ApplyFont(value); SavePreferences(); } } }
    public bool IsEnglish { get => SelectedLanguage == "English"; set { if (value) SelectedLanguage = "English"; } }
    public bool IsGerman { get => SelectedLanguage == "Deutsch"; set { if (value) SelectedLanguage = "Deutsch"; } }
    public bool IsSystemTheme { get => SelectedTheme == "System"; set { if (value) SelectedTheme = "System"; } }
    public bool IsDarkTheme { get => SelectedTheme == "Dark"; set { if (value) SelectedTheme = "Dark"; } }
    public bool IsLightTheme { get => SelectedTheme == "Light"; set { if (value) SelectedTheme = "Light"; } }
    public bool IsCascadiaCode { get => SelectedFont == "Cascadia Code"; set { if (value) SelectedFont = "Cascadia Code"; } }
    public bool IsVerdana { get => SelectedFont == "Verdana"; set { if (value) SelectedFont = "Verdana"; } }
    public CommitHistoryItem? SelectedCommit { get => _selectedCommit; set { if (Set(ref _selectedCommit, value)) _ = LoadAnalysisAsync(true); } }
    public AnalysisFileViewModel? SelectedFile { get => _selectedFile; set { if (Set(ref _selectedFile, value)) { ShowFile(value); OnPropertyChanged(nameof(SelectedAnalysisTitle)); OnPropertyChanged(nameof(SelectedAnalysisDescription)); } } }
    public string SelectedAnalysisTitle => SelectedFile?.GroupTitle ?? Groups.FirstOrDefault()?.Title ?? string.Empty;
    public string SelectedAnalysisDescription => SelectedFile?.GroupDescription ?? Groups.FirstOrDefault()?.Description ?? string.Empty;
    public RecentRepositoryItem? SelectedRecent { get => RecentRepositories.FirstOrDefault(item => item.Path == _selectedRecent); set { if (Set(ref _selectedRecent, value?.Path) && value is not null) _ = OpenRepositoryAsync(value.Path); } }
    public string ViewTitle => SelectedCommit is null ? "Current changes" : $"{SelectedCommit.Commit.Subject} · {SelectedCommit.Commit.Id[..Math.Min(8, SelectedCommit.Commit.Id.Length)]}";
    private async Task OpenRepositoryAsync() { var picker = new OpenFolderDialog { Title = "Open Git repository" }; if (picker.ShowDialog() == true) await OpenRepositoryAsync(picker.FolderName); }
    private async Task OpenRepositoryAsync(string path) { if (!await _git.IsGitAvailableAsync()) { StatusMessage = "Git is not installed or unavailable on PATH."; return; } RepositoryPath = path; AddRecent(path); StartWatcher(path); await RefreshAsync(true); }
    private async Task CloneAsync() { var source = InputDialog.Ask("Clone repository", "Repository URL or local source:"); if (string.IsNullOrWhiteSpace(source)) return; var destination = InputDialog.Ask("Clone repository", "Destination folder:"); if (string.IsNullOrWhiteSpace(destination)) return; var result = await _git.CloneAsync(source, destination); StatusMessage = result.IsSuccess ? "Repository cloned." : result.ErrorMessage; if (result.IsSuccess) await OpenRepositoryAsync(destination); }
    private async Task ShowCurrentAsync() { if (SelectedCommit is null) await LoadAnalysisAsync(true); else SelectedCommit = null; }
    private async Task RefreshAsync(bool analyzeWithAi)
    {
        if (string.IsNullOrWhiteSpace(RepositoryPath) || _loading) return; _loading = true;
        var refreshAnalysis = false;
        try { var snapshot = await _git.LoadAsync(RepositoryPath); History.Reset(snapshot.History.Select(commit => new CommitHistoryItem(commit))); StatusMessage = snapshot.State == RepositoryState.Ready ? $"{snapshot.Changes.Count} current file changes." : $"Git state: {snapshot.State}. Resolve it before editing."; refreshAnalysis = SelectedCommit is null || analyzeWithAi; }
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
        IsAnalyzing = analyzeWithAi;
        try
        {
            var scope = SelectedCommit is null ? DiffScope.WorkingTree : DiffScope.Historical;
            var diff = SelectedCommit is null ? (await _git.GetDiffAsync(RepositoryPath, DiffScope.Staged, cancellationToken: cancellationToken)) + (await _git.GetDiffAsync(RepositoryPath, DiffScope.WorkingTree, cancellationToken: cancellationToken)) : await _git.GetCommitDiffAsync(RepositoryPath, SelectedCommit.Commit.Id, cancellationToken: cancellationToken);
            if (analyzeWithAi) StatusMessage = SelectedLanguage == "Deutsch" ? "GPT-6 Luna analysiert die Änderungen..." : "GPT-6 Luna is analysing the changes...";
            var request = new DiffAnalysisRequest(RepositoryPath, scope, diff, SelectedLanguage == "Deutsch" ? "de" : "en");
            var result = await (analyzeWithAi ? _analysis : _heuristicAnalysis).AnalyzeAsync(request, cancellationToken);
            if (generation != Interlocked.Read(ref _analysisGeneration)) return;
            Groups.Reset(result.Groups.Select(group => new AnalysisGroupViewModel(group)));
            var firstFile = Groups.SelectMany(group => group.Files).FirstOrDefault();
            if (!Equals(SelectedFile, firstFile)) SelectedFile = firstFile; else ShowFile(firstFile);
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
    private Task AnalyzeWithAiAsync() => LoadAnalysisAsync(true);
    private Task CancelAnalysisAsync() { _analysisCancellation?.Cancel(); return Task.CompletedTask; }
    private async Task CheckCodexAsync() { try { var status = await _codex.GetStatusAsync(); SetCodexStatus(status); if (status.IsReady) await RefreshCodexAccountAsync(); } catch (Exception) { SetCodexStatus(new CodexCliStatus(CodexCliStatusKind.Unavailable, string.Empty)); } }
    private async Task LoginToCodexAsync() { AiStatusMessage = SelectedLanguage == "Deutsch" ? "ChatGPT-Anmeldung wird geöffnet..." : "Opening ChatGPT sign-in..."; try { var status = await _codex.LoginAsync(); SetCodexStatus(status); if (status.IsReady) await RefreshCodexAccountAsync(); } catch (Exception) { SetCodexStatus(new CodexCliStatus(CodexCliStatusKind.Unavailable, string.Empty)); } }
    private async Task RefreshCodexAccountAsync()
    {
        var usage = await _codex.GetAccountUsageAsync();
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
    private void SelectFile(AnalysisFileViewModel file) { SelectedFile = file; ShowFile(file); }
    private static void OpenUrl(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    private void ShowFile(AnalysisFileViewModel? file) { DiffLines.Clear(); if (file is null) return; foreach (var line in file.Diff.Split('\n')) DiffLines.Add(DiffLineViewModel.From(line)); }
    private void StartWatcher(string path) { _watcher?.Dispose(); _watcher = new FileSystemWatcher(path) { IncludeSubdirectories = true, EnableRaisingEvents = true }; _watcher.Changed += (_, _) => ScheduleRefresh(); _watcher.Created += (_, _) => ScheduleRefresh(); _watcher.Deleted += (_, _) => ScheduleRefresh(); _watcher.Renamed += (_, _) => ScheduleRefresh(); }
    private void ScheduleRefresh() { _analysisCancellation?.Cancel(); _refreshTimer.Stop(); _refreshTimer.Start(); } private async void OnRefreshTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e) => await Application.Current.Dispatcher.InvokeAsync(() => RefreshAsync(false)).Task.Unwrap();
    private static void ApplyFont(string font) => Application.Current.Resources["AppFontFamily"] = new FontFamily(font);
    private void AddRecent(string path) { var existing = RecentRepositories.FirstOrDefault(item => item.Path == path); if (existing is not null) RecentRepositories.Remove(existing); RecentRepositories.Insert(0, new RecentRepositoryItem(path)); while (RecentRepositories.Count > 10) RecentRepositories.RemoveAt(RecentRepositories.Count - 1); SavePreferences(); } private void SavePreferences() => _settings.Save(RecentRepositories.Select(item => item.Path), SelectedTheme, SelectedLanguage, SelectedFont);
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? property = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnPropertyChanged(property); return true; } private void OnPropertyChanged([CallerMemberName] string? property = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property)); public void Dispose() { _analysisCancellation?.Cancel(); _analysisCancellation?.Dispose(); _watcher?.Dispose(); _refreshTimer.Dispose(); }
}
public sealed class AnalysisGroupViewModel { public AnalysisGroupViewModel(WorkStepGroup group) { Title = group.Title; Description = group.Description; Files = group.Hunks.GroupBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase).Select(x => new AnalysisFileViewModel(x.Key, string.Join("\n", x.Select(h => $"{h.Header}\n{h.Content}")), group.Title, group.Description)).ToList(); } public string Title { get; } public string Description { get; } public IReadOnlyList<AnalysisFileViewModel> Files { get; } }
public sealed record AnalysisFileViewModel(string Path, string Diff, string GroupTitle, string GroupDescription);
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
public sealed record CodeToken(string Text, Brush Foreground);
public sealed record DiffLineViewModel(IReadOnlyList<CodeToken> Tokens, Brush? Background)
{
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal) { "abstract", "async", "await", "bool", "class", "const", "else", "enum", "false", "for", "foreach", "if", "in", "interface", "internal", "namespace", "new", "null", "private", "protected", "public", "readonly", "record", "return", "static", "string", "struct", "throw", "true", "using", "void", "while" };
    private static readonly Regex TokenPattern = new("//.*|\"(?:\\\\.|[^\"])*\"|\\b[A-Za-z_][A-Za-z0-9_]*\\b|\\b\\d+(?:\\.\\d+)?\\b|\\s+|.", RegexOptions.Compiled);
    public static DiffLineViewModel From(string line)
    {
        var tokens = new List<CodeToken>();
        var content = line;
        Brush? background = null;
        if (line.StartsWith('+') && !line.StartsWith("+++")) { tokens.Add(new CodeToken("+", Brush("DiffAdded"))); content = line[1..]; background = Brush("DiffAddedBackground"); }
        else if (line.StartsWith('-') && !line.StartsWith("---")) { tokens.Add(new CodeToken("-", Brush("DiffRemoved"))); content = line[1..]; background = Brush("DiffRemovedBackground"); }
        else if (line.StartsWith("@@")) return new([new CodeToken(line, Brush("DiffHunk"))], null);
        else if (line.StartsWith("diff ") || line.StartsWith("+++") || line.StartsWith("---")) return new([new CodeToken(line, Brush("Secondary"))], null);
        var matches = TokenPattern.Matches(content);
        for (var index = 0; index < matches.Count; index++)
        {
            var value = matches[index].Value;
            var color = value.StartsWith("//") ? "CodeComment" : value.StartsWith('"') ? "CodeString" : Keywords.Contains(value) ? "CodeKeyword" : char.IsUpper(value.FirstOrDefault()) ? "CodeType" : IsMethod(matches, index) ? "CodeMember" : "Primary";
            tokens.Add(new CodeToken(value, Brush(color)));
        }
        return new(tokens, background);
    }
    private static bool IsMethod(MatchCollection matches, int index) { for (var i = index + 1; i < matches.Count; i++) { if (string.IsNullOrWhiteSpace(matches[i].Value)) continue; return matches[i].Value == "("; } return false; }
    private static Brush Brush(string key) => (Brush)Application.Current.Resources[key];
}
internal static class CollectionExtensions { public static void Reset<T>(this ObservableCollection<T> items, IEnumerable<T> values) { items.Clear(); foreach (var value in values) items.Add(value); } }
