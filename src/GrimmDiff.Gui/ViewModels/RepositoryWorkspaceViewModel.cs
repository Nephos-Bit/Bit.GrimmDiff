using System.IO;
using System.Windows;
using GrimmDiff.Gui.Abstracts.Services;
using GrimmDiff.Gui.Commands;
using GrimmDiff.Gui.Models;
using GrimmDiff.Gui.Services;
using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.UseCases;

namespace GrimmDiff.Gui.ViewModels;

public sealed class RepositoryWorkspaceViewModel : ObservableViewModel, IDisposable
{
    private readonly RepositoryWorkspaceService _workspace;
    private readonly PreferencesStateService _preferences;
    private readonly IUserDialogService _dialogs;
    private readonly IExternalNavigationService _navigation;
    private readonly RepositoryRefreshScheduler _refreshScheduler;
    private readonly WorkspaceStatusViewModel _status;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private Task _contextChangeTask = Task.CompletedTask;
    private string _repositoryPath = string.Empty;
    private string _currentBranch = string.Empty;
    private string? _selectedRecent;
    private CommitHistoryItemModel? _selectedCommit;
    private bool _operationRunning, _branchMenuOpen, _panelExpanded = true;
    private GridLength _panelWidth = new(300), _expandedWidth = new(300);

    public RepositoryWorkspaceViewModel(RepositoryWorkspaceService workspace, PreferencesStateService preferences,
        IUserDialogService dialogs, IExternalNavigationService navigation, IRepositoryChangeMonitorService monitor,
        WorkspaceStatusViewModel status)
    {
        _workspace = workspace;
        _preferences = preferences;
        _dialogs = dialogs;
        _navigation = navigation;
        _refreshScheduler = new RepositoryRefreshScheduler(monitor, () => RefreshScheduled?.Invoke(), RefreshAsync);
        _status = status;
        _preferences.PropertyChanged += OnPreferencesChanged;
        OpenRepositoryCommand = new AsyncCommand(OpenRepositoryAsync);
        CloneCommand = new AsyncCommand(CloneAsync);
        ShowCurrentCommand = new AsyncCommand(ShowCurrentAsync);
        PullCommand = new AsyncCommand(PullAsync, CanRunOperation);
        SwitchBranchCommand = new AsyncCommandOfT<BranchInfoModel>(SwitchBranchAsync,
            branch => CanRunOperation() && !branch.IsCurrent);
        OpenBranchMenuCommand = new DelegateCommand<object>(_ => IsBranchMenuOpen = true);
        ToggleRepositoryPanelCommand = new DelegateCommand<object>(_ => IsRepositoryPanelExpanded = !IsRepositoryPanelExpanded);
        OpenUrlCommand = new DelegateCommand<string>(_navigation.Open);
    }

    public event Func<Task>? ContextChanged;
    public event Action? RefreshScheduled;
    public BulkObservableCollectionModel<RecentRepositoryItemModel> RecentRepositories { get; } = [];
    public BulkObservableCollectionModel<CommitHistoryItemModel> History { get; } = [];
    public BulkObservableCollectionModel<BranchInfoModel> Branches { get; } = [];
    public AsyncCommand OpenRepositoryCommand { get; }
    public AsyncCommand CloneCommand { get; }
    public AsyncCommand ShowCurrentCommand { get; }
    public AsyncCommand PullCommand { get; }
    public AsyncCommandOfT<BranchInfoModel> SwitchBranchCommand { get; }
    public DelegateCommand<object> OpenBranchMenuCommand { get; }
    public DelegateCommand<object> ToggleRepositoryPanelCommand { get; }
    public DelegateCommand<string> OpenUrlCommand { get; }
    public string RepositoryPath
    {
        get => _repositoryPath;
        private set
        {
            if (!Set(ref _repositoryPath, value)) return;
            OnPropertyChanged(nameof(RepositoryName));
            OnPropertyChanged(nameof(HasRepository));
            PullCommand.RaiseCanExecuteChanged();
            SwitchBranchCommand.RaiseCanExecuteChanged();
        }
    }
    public string RepositoryName => string.IsNullOrWhiteSpace(RepositoryPath)
        ? string.Empty : Path.GetFileName(Path.TrimEndingDirectorySeparator(RepositoryPath));
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
        ? _preferences.Text["NoBranch"] : $"{_preferences.Text["Branch"]}: {CurrentBranch}";
    public string PullToolTip => _preferences.Text["Pull"];
    public bool IsBranchMenuOpen
    {
        get => _branchMenuOpen;
        set => Set(ref _branchMenuOpen, value);
    }
    public bool IsRepositoryPanelExpanded
    {
        get => _panelExpanded;
        set
        {
            if (!Set(ref _panelExpanded, value)) return;
            RepositoryPanelColumnWidth = value ? _expandedWidth : new GridLength(0);
        }
    }
    public GridLength RepositoryPanelColumnWidth
    {
        get => _panelWidth;
        set
        {
            if (!Set(ref _panelWidth, value)) return;
            if (value.Value > 0) _expandedWidth = value;
        }
    }
    public CommitHistoryItemModel? SelectedCommit
    {
        get => _selectedCommit;
        set
        {
            if (Set(ref _selectedCommit, value)) _ = StartContextChange();
        }
    }
    public RecentRepositoryItemModel? SelectedRecent
    {
        get => RecentRepositories.FirstOrDefault(item => item.Path == _selectedRecent);
        set
        {
            if (Set(ref _selectedRecent, value?.Path) && value is not null) _ = OpenRepositoryAsync(value.Path);
        }
    }
    public string ViewTitle => SelectedCommit is null ? "Current changes" :
        $"{SelectedCommit.Commit.Subject} · {SelectedCommit.Commit.Id[..Math.Min(8, SelectedCommit.Commit.Id.Length)]}";

    public async Task OpenRepositoryAsync(string path)
    {
        if (!await _workspace.IsGitAvailableAsync())
        {
            _status.Message = "Git is not installed or unavailable on PATH.";
            return;
        }
        RefreshScheduled?.Invoke();
        RepositoryPath = path;
        Branches.Clear();
        CurrentBranch = string.Empty;
        _selectedCommit = null;
        OnPropertyChanged(nameof(SelectedCommit));
        OnPropertyChanged(nameof(ViewTitle));
        _preferences.AddRecent(path);
        _refreshScheduler.Watch(path);
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (!HasRepository) return;
        await _refreshGate.WaitAsync();
        var path = RepositoryPath;
        try
        {
            var snapshot = await _workspace.OpenAsync(path);
            if (path != RepositoryPath) return;
            History.ReplaceAll(snapshot.History.Select(commit => new CommitHistoryItemModel(commit, _preferences.TicketLinks)));
            Branches.ReplaceAll(snapshot.Branches);
            CurrentBranch = snapshot.Branch;
            SwitchBranchCommand.RaiseCanExecuteChanged();
            _status.Message = snapshot.State == RepositoryStateModel.Ready
                ? $"{snapshot.Changes.Count} current file changes."
                : $"Git state: {snapshot.State}. Resolve it before editing.";
            if (SelectedCommit is null) await StartContextChange();
        }
        catch (Exception ex)
        {
            if (path == RepositoryPath) _status.Message = ex.Message;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private async Task OpenRepositoryAsync()
    {
        var path = _dialogs.SelectRepository();
        if (!string.IsNullOrWhiteSpace(path)) await OpenRepositoryAsync(path);
    }
    private async Task CloneAsync()
    {
        var source = _dialogs.Ask("Clone repository", "Repository URL or local source:");
        if (string.IsNullOrWhiteSpace(source)) return;
        var destination = _dialogs.Ask("Clone repository", "Destination folder:");
        if (string.IsNullOrWhiteSpace(destination)) return;
        var result = await _workspace.CloneAsync(source, destination);
        _status.Message = result.IsSuccess ? "Repository cloned." : result.ErrorMessage;
        if (result.IsSuccess) await OpenRepositoryAsync(destination);
    }
    private Task ShowCurrentAsync()
    {
        if (SelectedCommit is null) return StartContextChange();
        SelectedCommit = null;
        return _contextChangeTask;
    }
    private Task StartContextChange()
    {
        var callbacks = ContextChanged?.GetInvocationList().Cast<Func<Task>>();
        return _contextChangeTask = callbacks is null
            ? Task.CompletedTask
            : Task.WhenAll(callbacks.Select(callback => callback()));
    }
    private bool CanRunOperation() => HasRepository && !_operationRunning;
    private Task PullAsync() => RunOperationAsync(() => _workspace.PullAsync(RepositoryPath),
        _preferences.Language == "Deutsch" ? "Änderungen werden geholt..." : "Pulling changes...");
    private Task SwitchBranchAsync(BranchInfoModel branch) => branch.IsCurrent ? Task.CompletedTask :
        RunOperationAsync(() => _workspace.SwitchBranchAsync(RepositoryPath, branch.Name),
            _preferences.Language == "Deutsch" ? $"Wechsle zu {branch.Name}..." : $"Switching to {branch.Name}...");
    private async Task RunOperationAsync(Func<Task<GitCommandResultModel>> operation, string progress)
    {
        if (!CanRunOperation()) return;
        _operationRunning = true;
        PullCommand.RaiseCanExecuteChanged();
        SwitchBranchCommand.RaiseCanExecuteChanged();
        try
        {
            _status.Message = progress;
            var result = await operation();
            if (!result.IsSuccess)
            {
                _status.Message = result.ErrorMessage;
                return;
            }
            _selectedCommit = null;
            OnPropertyChanged(nameof(SelectedCommit));
            OnPropertyChanged(nameof(ViewTitle));
            await RefreshAsync();
        }
        finally
        {
            _operationRunning = false;
            PullCommand.RaiseCanExecuteChanged();
            SwitchBranchCommand.RaiseCanExecuteChanged();
        }
    }

    private void OnPreferencesChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PreferencesStateService.RecentRepositories) or "")
            RecentRepositories.ReplaceAll(_preferences.RecentRepositories.Select(path => new RecentRepositoryItemModel(path)));
        if (e.PropertyName is nameof(PreferencesStateService.TicketLinks) or "") RefreshHistoryTicketLinks();
        if (e.PropertyName is nameof(PreferencesStateService.Language) or "")
        {
            OnPropertyChanged(nameof(BranchToolTip));
            OnPropertyChanged(nameof(PullToolTip));
        }
    }
    private void RefreshHistoryTicketLinks()
    {
        if (History.Count == 0) return;
        var selectedId = _selectedCommit?.Commit.Id;
        History.ReplaceAll(History.Select(item => new CommitHistoryItemModel(item.Commit, _preferences.TicketLinks)));
        _selectedCommit = selectedId is null ? null : History.FirstOrDefault(item => item.Commit.Id == selectedId);
        OnPropertyChanged(nameof(SelectedCommit));
        OnPropertyChanged(nameof(ViewTitle));
    }
    public void Dispose()
    {
        _preferences.PropertyChanged -= OnPreferencesChanged;
        _refreshScheduler.Dispose();
        _refreshGate.Dispose();
    }
}
