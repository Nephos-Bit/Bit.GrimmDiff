using System.Windows;
using GrimmDiff.Gui.Commands;
using GrimmDiff.Gui.Models;
using GrimmDiff.Gui.Services;
using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.UseCases;

namespace GrimmDiff.Gui.ViewModels;

public sealed class AnalysisWorkspaceViewModel : ObservableViewModel, IDisposable
{
    private readonly RepositoryWorkspaceViewModel _repository;
    private readonly DiffAnalysisService _analysis;
    private readonly DiffDocumentViewModel _diff;
    private readonly CodexAccountViewModel _account;
    private readonly AiConsentService _consent;
    private readonly PreferencesStateService _preferences;
    private readonly WorkspaceStatusViewModel _status;
    private CancellationTokenSource? _cancellation;
    private long _generation;
    private bool _isAnalyzing, _isStale;
    private string _sourceLabel = "No analysis yet.";
    private string _disclosure = "AI analysis has not been prepared.";
    private AnalysisFileViewModel? _selectedFile;

    public AnalysisWorkspaceViewModel(RepositoryWorkspaceViewModel repository, DiffAnalysisService analysis,
        DiffDocumentViewModel diff, CodexAccountViewModel account, AiConsentService consent,
        PreferencesStateService preferences, WorkspaceStatusViewModel status)
    {
        _repository = repository;
        _analysis = analysis;
        _diff = diff;
        _account = account;
        _consent = consent;
        _preferences = preferences;
        _status = status;
        AnalyzeWithAiCommand = new AsyncCommand(AnalyzeWithAiAsync, () => _repository.HasRepository && !IsAnalyzing);
        CancelAnalysisCommand = new AsyncCommand(() => { Cancel(); return Task.CompletedTask; }, () => IsAnalyzing);
        SelectFileCommand = new DelegateCommand<AnalysisFileViewModel>(file => SelectedFile = file);
        _repository.PropertyChanged += OnRepositoryChanged;
        _preferences.PropertyChanged += OnPreferencesChanged;
    }

    public BulkObservableCollectionModel<AnalysisGroupViewModel> Groups { get; } = [];
    public CodexAccountViewModel Account => _account;
    public AsyncCommand AnalyzeWithAiCommand { get; }
    public AsyncCommand CancelAnalysisCommand { get; }
    public DelegateCommand<AnalysisFileViewModel> SelectFileCommand { get; }
    public string ViewTitle => _repository.ViewTitle;
    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        private set
        {
            if (!Set(ref _isAnalyzing, value)) return;
            _account.SetAnalysisRunning(value);
            AnalyzeWithAiCommand.RaiseCanExecuteChanged();
            CancelAnalysisCommand.RaiseCanExecuteChanged();
        }
    }
    public bool IsAnalysisStale
    {
        get => _isStale;
        private set => Set(ref _isStale, value);
    }
    public string AnalysisSourceLabel
    {
        get => _sourceLabel;
        private set => Set(ref _sourceLabel, value);
    }
    public string AiDisclosureText
    {
        get => _disclosure;
        private set => Set(ref _disclosure, value);
    }
    public string AnalysisProgressText => _preferences.Language == "Deutsch"
        ? "GPT-6 Luna analysiert die Änderungen..." : "GPT-6 Luna is analysing the changes...";
    public string AnalyzeButtonText => _preferences.Language == "Deutsch"
        ? "Mit GPT-6 Luna analysieren" : "Analyse with GPT-6 Luna";
    public string CancelButtonText => _preferences.Language == "Deutsch" ? "Abbrechen" : "Cancel";
    public Visibility MascotVisibility => _preferences.MascotsEnabled ? Visibility.Visible : Visibility.Collapsed;
    public string SelectedMascotId => _preferences.MascotId;
    public AnalysisFileViewModel? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (!Set(ref _selectedFile, value)) return;
            _ = _diff.ShowFileAsync(value);
            OnPropertyChanged(nameof(SelectedAnalysisTitle));
            OnPropertyChanged(nameof(SelectedAnalysisDescription));
        }
    }
    public string SelectedAnalysisTitle => SelectedFile?.GroupTitle ?? Groups.FirstOrDefault()?.Title ?? string.Empty;
    public string SelectedAnalysisDescription => SelectedFile?.GroupDescription ??
                                                 Groups.FirstOrDefault()?.Description ?? string.Empty;

    public void Cancel() => _cancellation?.Cancel();

    public async Task LoadAsync(bool useAi = false)
    {
        if (!_repository.HasRepository) return;
        var generation = Interlocked.Increment(ref _generation);
        _cancellation?.Cancel();
        _cancellation?.Dispose();
        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;
        var query = new DiffAnalysisQueryModel(_repository.RepositoryPath, _repository.SelectedCommit?.Commit.Id,
            _preferences.Language == "Deutsch" ? "de" : "en", useAi);
        IsAnalyzing = useAi;
        if (useAi) _status.Message = AnalysisProgressText;
        try
        {
            var result = await _analysis.AnalyzeAsync(query, token);
            var groups = await Task.Run(() => result.Groups.Select(group => new AnalysisGroupViewModel(group)).ToList(), token);
            if (generation != Interlocked.Read(ref _generation)) return;
            if (useAi && result.AiPreview is not null) SetDisclosure(result.AiPreview);
            var previous = SelectedFile;
            Groups.ReplaceAll(groups);
            var selected = previous is null ? null : Groups.SelectMany(group => group.Files).FirstOrDefault(file =>
                file.Path.Equals(previous.Path, StringComparison.OrdinalIgnoreCase) &&
                file.GroupTitle.Equals(previous.GroupTitle, StringComparison.Ordinal));
            selected ??= Groups.SelectMany(group => group.Files).FirstOrDefault();
            if (ReferenceEquals(SelectedFile, selected)) _ = _diff.ShowFileAsync(selected);
            else SelectedFile = selected;
            OnPropertyChanged(nameof(SelectedAnalysisTitle));
            OnPropertyChanged(nameof(SelectedAnalysisDescription));
            IsAnalysisStale = !useAi;
            AnalysisSourceLabel = result.Source == DiffAnalysisSourceModel.Codex
                ? German ? "GPT-6 Luna · KI-Gruppierung" : "GPT-6 Luna · AI grouping"
                : useAi ? German ? "Regelbasierter Fallback" : "Rule-based fallback"
                : German ? "Regelbasierte Vorschau · KI-Neuanalyse verfügbar" : "Rule-based preview · AI refresh available";
            var summary = result.Hunks.Count == 0
                ? German ? "Keine analysierbaren Änderungen." : "No textual changes to analyse."
                : German
                    ? $"{result.Hunks.Count} geänderte Abschnitte wurden in {result.Groups.Count} Arbeitsschritte gruppiert."
                    : $"{result.Hunks.Count} changed sections grouped into {result.Groups.Count} work steps.";
            _status.Message = string.IsNullOrWhiteSpace(result.Diagnostic) ? summary : $"{result.Diagnostic} {summary}";
        }
        catch (OperationCanceledException)
        {
            if (generation == Interlocked.Read(ref _generation))
                _status.Message = German ? "Analyse abgebrochen." : "Analysis cancelled.";
        }
        catch (Exception ex)
        {
            if (generation == Interlocked.Read(ref _generation)) _status.Message = ex.Message;
        }
        finally
        {
            if (generation == Interlocked.Read(ref _generation))
            {
                IsAnalyzing = false;
                _cancellation?.Dispose();
                _cancellation = null;
            }
        }
    }

    private async Task AnalyzeWithAiAsync()
    {
        var query = new DiffAnalysisQueryModel(_repository.RepositoryPath, _repository.SelectedCommit?.Commit.Id,
            German ? "de" : "en", true);
        var preview = await _analysis.PreviewAiAsync(query);
        SetDisclosure(preview);
        if (_consent.Approve(_repository.RepositoryPath, preview)) await LoadAsync(true);
    }
    private void SetDisclosure(AiAnalysisPreviewModel preview)
    {
        AiDisclosureText = German
            ? $"KI-Datenübersicht: {preview.TransferableSections} von {preview.TotalSections} Abschnitten ({preview.TransferableCharacters:N0} Zeichen) werden an ChatGPT übertragen; {preview.ExcludedSections} bleiben lokal."
            : $"AI data overview: {preview.TransferableSections} of {preview.TotalSections} sections ({preview.TransferableCharacters:N0} characters) are sent to ChatGPT; {preview.ExcludedSections} stay local.";
    }
    private bool German => _preferences.Language == "Deutsch";
    private void OnRepositoryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(RepositoryWorkspaceViewModel.RepositoryPath))
            AnalyzeWithAiCommand.RaiseCanExecuteChanged();
        if (e.PropertyName is nameof(RepositoryWorkspaceViewModel.SelectedCommit) or nameof(RepositoryWorkspaceViewModel.ViewTitle))
            OnPropertyChanged(nameof(ViewTitle));
    }
    private void OnPreferencesChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PreferencesStateService.Language) or "")
        {
            OnPropertyChanged(nameof(AnalysisProgressText));
            OnPropertyChanged(nameof(AnalyzeButtonText));
            OnPropertyChanged(nameof(CancelButtonText));
            _ = LoadAsync();
        }
        if (e.PropertyName is nameof(PreferencesStateService.MascotsEnabled) or "")
            OnPropertyChanged(nameof(MascotVisibility));
        if (e.PropertyName is nameof(PreferencesStateService.MascotId) or "")
            OnPropertyChanged(nameof(SelectedMascotId));
    }
    public void Dispose()
    {
        _repository.PropertyChanged -= OnRepositoryChanged;
        _preferences.PropertyChanged -= OnPreferencesChanged;
        _cancellation?.Cancel();
        _cancellation?.Dispose();
    }
}
