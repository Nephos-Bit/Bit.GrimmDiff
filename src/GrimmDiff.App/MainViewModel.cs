using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using GrimmDiff.Domain;

namespace GrimmDiff.App;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IGitRepositoryService _git; private readonly IDiffAnalysisProvider _analysis; private readonly AppSettingsStore _settings = new();
    private readonly System.Timers.Timer _refreshTimer = new(500) { AutoReset = false }; private FileSystemWatcher? _watcher;
    private string _repositoryPath = string.Empty, _statusMessage = "Select a repository to inspect its changes.", _selectedLanguage = "English", _selectedTheme = "System";
    private CommitInfo? _selectedCommit; private AnalysisFileViewModel? _selectedFile; private string? _selectedRecent; private bool _loading;
    public MainViewModel(IGitRepositoryService git, IDiffAnalysisProvider analysis)
    {
        _git = git; _analysis = analysis; _refreshTimer.Elapsed += OnRefreshTimerElapsed;
        var saved = _settings.Load(); _selectedLanguage = saved.Language; _selectedTheme = saved.Theme; ThemeService.Apply(_selectedTheme);
        foreach (var item in saved.RecentRepositories.Where(Directory.Exists)) RecentRepositories.Add(item);
        OpenRepositoryCommand = new AsyncCommand(OpenRepositoryAsync); CloneCommand = new AsyncCommand(CloneAsync); ShowCurrentCommand = new AsyncCommand(ShowCurrentAsync); SelectFileCommand = new DelegateCommand<AnalysisFileViewModel>(SelectFile);
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<string> RecentRepositories { get; } = []; public ObservableCollection<CommitInfo> History { get; } = []; public ObservableCollection<AnalysisGroupViewModel> Groups { get; } = []; public ObservableCollection<DiffLineViewModel> DiffLines { get; } = [];
    public IReadOnlyList<string> Languages { get; } = ["English", "Deutsch"]; public IReadOnlyList<string> Themes { get; } = ["System", "Dark", "Light"]; public LocalizedText Text => new(() => SelectedLanguage);
    public AsyncCommand OpenRepositoryCommand { get; } public AsyncCommand CloneCommand { get; } public AsyncCommand ShowCurrentCommand { get; } public DelegateCommand<AnalysisFileViewModel> SelectFileCommand { get; }
    public string RepositoryPath { get => _repositoryPath; private set => Set(ref _repositoryPath, value); } public string StatusMessage { get => _statusMessage; private set => Set(ref _statusMessage, value); }
    public string SelectedLanguage { get => _selectedLanguage; set { if (Set(ref _selectedLanguage, value)) { OnPropertyChanged(nameof(Text)); OnPropertyChanged(nameof(IsEnglish)); OnPropertyChanged(nameof(IsGerman)); SavePreferences(); _ = LoadAnalysisAsync(); } } }
    public string SelectedTheme { get => _selectedTheme; set { if (Set(ref _selectedTheme, value)) { OnPropertyChanged(nameof(IsSystemTheme)); OnPropertyChanged(nameof(IsDarkTheme)); OnPropertyChanged(nameof(IsLightTheme)); ThemeService.Apply(value); SavePreferences(); } } }
    public bool IsEnglish { get => SelectedLanguage == "English"; set { if (value) SelectedLanguage = "English"; } }
    public bool IsGerman { get => SelectedLanguage == "Deutsch"; set { if (value) SelectedLanguage = "Deutsch"; } }
    public bool IsSystemTheme { get => SelectedTheme == "System"; set { if (value) SelectedTheme = "System"; } }
    public bool IsDarkTheme { get => SelectedTheme == "Dark"; set { if (value) SelectedTheme = "Dark"; } }
    public bool IsLightTheme { get => SelectedTheme == "Light"; set { if (value) SelectedTheme = "Light"; } }
    public CommitInfo? SelectedCommit { get => _selectedCommit; set { if (Set(ref _selectedCommit, value)) _ = LoadAnalysisAsync(); } }
    public AnalysisFileViewModel? SelectedFile { get => _selectedFile; set { if (Set(ref _selectedFile, value)) ShowFile(value); } }
    public string? SelectedRecent { get => _selectedRecent; set { if (Set(ref _selectedRecent, value) && value is not null) _ = OpenRepositoryAsync(value); } }
    public string ViewTitle => SelectedCommit is null ? "Current changes" : $"{SelectedCommit.Subject} · {SelectedCommit.Id[..Math.Min(8, SelectedCommit.Id.Length)]}";
    private async Task OpenRepositoryAsync() { var picker = new OpenFolderDialog { Title = "Open Git repository" }; if (picker.ShowDialog() == true) await OpenRepositoryAsync(picker.FolderName); }
    private async Task OpenRepositoryAsync(string path) { if (!await _git.IsGitAvailableAsync()) { StatusMessage = "Git is not installed or unavailable on PATH."; return; } RepositoryPath = path; AddRecent(path); StartWatcher(path); await RefreshAsync(); }
    private async Task CloneAsync() { var source = InputDialog.Ask("Clone repository", "Repository URL or local source:"); if (string.IsNullOrWhiteSpace(source)) return; var destination = InputDialog.Ask("Clone repository", "Destination folder:"); if (string.IsNullOrWhiteSpace(destination)) return; var result = await _git.CloneAsync(source, destination); StatusMessage = result.IsSuccess ? "Repository cloned." : result.ErrorMessage; if (result.IsSuccess) await OpenRepositoryAsync(destination); }
    private async Task ShowCurrentAsync() { SelectedCommit = null; await LoadAnalysisAsync(); }
    private async Task RefreshAsync()
    {
        if (string.IsNullOrWhiteSpace(RepositoryPath) || _loading) return; _loading = true;
        try { var snapshot = await _git.LoadAsync(RepositoryPath); History.Reset(snapshot.History); StatusMessage = snapshot.State == RepositoryState.Ready ? $"{snapshot.Changes.Count} current file changes." : $"Git state: {snapshot.State}. Resolve it before editing."; await LoadAnalysisAsync(); }
        catch (Exception ex) { StatusMessage = ex.Message; } finally { _loading = false; }
    }
    private async Task LoadAnalysisAsync()
    {
        if (string.IsNullOrWhiteSpace(RepositoryPath)) return;
        try
        {
            var scope = SelectedCommit is null ? DiffScope.WorkingTree : DiffScope.Historical;
            var diff = SelectedCommit is null ? (await _git.GetDiffAsync(RepositoryPath, DiffScope.Staged)) + (await _git.GetDiffAsync(RepositoryPath, DiffScope.WorkingTree)) : await _git.GetCommitDiffAsync(RepositoryPath, SelectedCommit.Id);
            var result = await _analysis.AnalyzeAsync(new DiffAnalysisRequest(scope, diff, SelectedLanguage == "Deutsch" ? "de" : "en")); Groups.Reset(result.Groups.Select(group => new AnalysisGroupViewModel(group))); DiffLines.Clear(); OnPropertyChanged(nameof(ViewTitle));
            StatusMessage = result.Hunks.Count == 0 ? "No textual changes to analyse." : $"{result.Hunks.Count} changed sections grouped into {result.Groups.Count} work steps.";
        }
        catch (Exception ex) { StatusMessage = ex.Message; }
    }
    private void SelectFile(AnalysisFileViewModel file) { SelectedFile = file; ShowFile(file); }
    private void ShowFile(AnalysisFileViewModel? file) { DiffLines.Clear(); if (file is null) return; foreach (var line in file.Diff.Split('\n')) DiffLines.Add(DiffLineViewModel.From(line)); }
    private void StartWatcher(string path) { _watcher?.Dispose(); _watcher = new FileSystemWatcher(path) { IncludeSubdirectories = true, EnableRaisingEvents = true }; _watcher.Changed += (_, _) => ScheduleRefresh(); _watcher.Created += (_, _) => ScheduleRefresh(); _watcher.Deleted += (_, _) => ScheduleRefresh(); _watcher.Renamed += (_, _) => ScheduleRefresh(); }
    private void ScheduleRefresh() { _refreshTimer.Stop(); _refreshTimer.Start(); } private async void OnRefreshTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e) => await Application.Current.Dispatcher.InvokeAsync(() => RefreshAsync()).Task.Unwrap();
    private void AddRecent(string path) { if (RecentRepositories.Contains(path)) RecentRepositories.Remove(path); RecentRepositories.Insert(0, path); while (RecentRepositories.Count > 10) RecentRepositories.RemoveAt(RecentRepositories.Count - 1); SavePreferences(); } private void SavePreferences() => _settings.Save(RecentRepositories, SelectedTheme, SelectedLanguage);
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? property = null) { if (EqualityComparer<T>.Default.Equals(field, value)) return false; field = value; OnPropertyChanged(property); return true; } private void OnPropertyChanged([CallerMemberName] string? property = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property)); public void Dispose() { _watcher?.Dispose(); _refreshTimer.Dispose(); }
}
public sealed class AnalysisGroupViewModel { public AnalysisGroupViewModel(WorkStepGroup group) { Description = group.Description; Files = group.Hunks.GroupBy(x => x.FilePath, StringComparer.OrdinalIgnoreCase).Select(x => new AnalysisFileViewModel(x.Key, string.Join("\n", x.Select(h => $"{h.Header}\n{h.Content}")))).ToList(); } public string Description { get; } public IReadOnlyList<AnalysisFileViewModel> Files { get; } }
public sealed record AnalysisFileViewModel(string Path, string Diff);
public sealed record DiffLineViewModel(string Text, Brush Foreground) { public static DiffLineViewModel From(string line) => line.StartsWith('+') && !line.StartsWith("+++") ? new(line, Brushes.LightGreen) : line.StartsWith('-') && !line.StartsWith("---") ? new(line, Brushes.OrangeRed) : line.StartsWith("@@") ? new(line, Brushes.DeepSkyBlue) : line.StartsWith("diff ") || line.StartsWith("+++") || line.StartsWith("---") ? new(line, Brushes.DarkGray) : new(line, Brushes.Gainsboro); }
internal static class CollectionExtensions { public static void Reset<T>(this ObservableCollection<T> items, IEnumerable<T> values) { items.Clear(); foreach (var value in values) items.Add(value); } }
