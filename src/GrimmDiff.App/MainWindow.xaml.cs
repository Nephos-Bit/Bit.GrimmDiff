using System.Windows;
using GrimmDiff.Infrastructure;
namespace GrimmDiff.App;
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    public MainWindow()
    {
        InitializeComponent();
        var codex = new CodexCliClient();
        var heuristic = new LocalHeuristicDiffAnalysisProvider();
        _viewModel = new MainViewModel(
            new GitCliRepositoryService(),
            new CodexCliDiffAnalysisProvider(codex, heuristic),
            heuristic,
            codex);
        DataContext = _viewModel;
        ContentRendered += OnContentRendered;
        Closed += (_, _) => _viewModel.Dispose();
    }

    private async void OnContentRendered(object? sender, EventArgs e)
    {
        ContentRendered -= OnContentRendered;
        await _viewModel.InitializeAsync();
    }
}
