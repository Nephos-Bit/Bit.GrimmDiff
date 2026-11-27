using GrimmDiff.Gui.Commands;
using GrimmDiff.Gui.Models.Rendering;
using GrimmDiff.Gui.Renderers;
using GrimmDiff.Gui.Services;
using GrimmDiff.Logic.Abstracts.Models;

namespace GrimmDiff.Gui.ViewModels;

public sealed class DiffDocumentViewModel : ObservableViewModel, IDisposable
{
    private readonly PreferencesStateService _preferences;
    private CancellationTokenSource? _renderCancellation;
    private long _renderGeneration;
    private AnalysisFileViewModel? _file;
    private DiffRenderDocumentModel? _document;
    private bool _hasMoreDiff;

    public DiffDocumentViewModel(PreferencesStateService preferences)
    {
        _preferences = preferences;
        LoadMoreDiffCommand = new AsyncCommand(LoadMoreAsync, () => HasMoreDiff);
        _preferences.PropertyChanged += OnPreferencesChanged;
    }

    public AsyncCommand LoadMoreDiffCommand { get; }
    public DiffRenderDocumentModel? DisplayedDiff
    {
        get => _document;
        private set => Set(ref _document, value);
    }
    public bool HasMoreDiff
    {
        get => _hasMoreDiff;
        private set
        {
            if (Set(ref _hasMoreDiff, value)) LoadMoreDiffCommand.RaiseCanExecuteChanged();
        }
    }
    public bool HasSelectedReview => _file?.Review is not null;
    public string SelectedReviewStatusText => _file?.Review?.Status switch
    {
        DiffGroupReviewStatusModel.Good => _preferences.Language == "Deutsch" ? "Review: Gut" : "Review: Good",
        DiffGroupReviewStatusModel.Ok => "Review: OK",
        DiffGroupReviewStatusModel.Warning => _preferences.Language == "Deutsch" ? "Review: Warnung" : "Review: Warning",
        _ => string.Empty
    };
    public string SelectedReviewHint => _file?.Review?.Hint ?? string.Empty;
    public bool HasSelectedReviewHint => !string.IsNullOrWhiteSpace(SelectedReviewHint);
    public bool IsSelectedReviewGood => _file?.Review?.Status == DiffGroupReviewStatusModel.Good;
    public bool IsSelectedReviewOk => _file?.Review?.Status == DiffGroupReviewStatusModel.Ok;
    public bool IsSelectedReviewWarning => _file?.Review?.Status == DiffGroupReviewStatusModel.Warning;

    public async Task ShowFileAsync(AnalysisFileViewModel? file)
    {
        _file = file;
        NotifyReview();
        var (generation, token) = NextRender();
        if (file is null)
        {
            Publish(null);
            return;
        }
        if (file.RenderDocument is not null)
        {
            Publish(file.RenderDocument);
            return;
        }
        try
        {
            var page = await Task.Run(() => DiffRenderer.CreatePage(file.Hunks, cancellationToken: token), token);
            if (generation != Interlocked.Read(ref _renderGeneration) || !ReferenceEquals(file, _file)) return;
            file.RenderDocument = DiffRenderDocumentModel.FromPage(page);
            Publish(file.RenderDocument);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public Task RefreshThemeAsync()
    {
        if (_file is null) return Task.CompletedTask;
        _file.RenderDocument = null;
        return ShowFileAsync(_file);
    }

    private async Task LoadMoreAsync()
    {
        var file = _file;
        var document = file?.RenderDocument;
        if (file is null || document?.NextPage is null) return;
        var (generation, token) = NextRender();
        try
        {
            var page = await Task.Run(() => DiffRenderer.CreatePage(file.Hunks, document.NextPage, token), token);
            if (generation != Interlocked.Read(ref _renderGeneration) || !ReferenceEquals(file, _file) ||
                !ReferenceEquals(document, file.RenderDocument)) return;
            file.RenderDocument = document.Append(page);
            Publish(file.RenderDocument);
        }
        catch (OperationCanceledException)
        {
        }
    }
    private (long Generation, CancellationToken Token) NextRender()
    {
        var generation = Interlocked.Increment(ref _renderGeneration);
        _renderCancellation?.Cancel();
        _renderCancellation?.Dispose();
        _renderCancellation = new CancellationTokenSource();
        return (generation, _renderCancellation.Token);
    }
    private void Publish(DiffRenderDocumentModel? document)
    {
        DisplayedDiff = document;
        HasMoreDiff = document?.NextPage is not null;
    }
    private void NotifyReview()
    {
        OnPropertyChanged(nameof(HasSelectedReview));
        OnPropertyChanged(nameof(SelectedReviewStatusText));
        OnPropertyChanged(nameof(SelectedReviewHint));
        OnPropertyChanged(nameof(HasSelectedReviewHint));
        OnPropertyChanged(nameof(IsSelectedReviewGood));
        OnPropertyChanged(nameof(IsSelectedReviewOk));
        OnPropertyChanged(nameof(IsSelectedReviewWarning));
    }
    private void OnPreferencesChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PreferencesStateService.Language) or "")
            OnPropertyChanged(nameof(SelectedReviewStatusText));
        if (e.PropertyName is nameof(PreferencesStateService.Theme) or "") _ = RefreshThemeAsync();
    }
    public void Dispose()
    {
        _preferences.PropertyChanged -= OnPreferencesChanged;
        _renderCancellation?.Cancel();
        _renderCancellation?.Dispose();
    }
}
