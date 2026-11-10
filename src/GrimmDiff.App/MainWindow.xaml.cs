using System.Windows;
using GrimmDiff.Infrastructure;
namespace GrimmDiff.App;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var codex = new CodexCliClient();
        var heuristic = new LocalHeuristicDiffAnalysisProvider();
        DataContext = new MainViewModel(
            new GitCliRepositoryService(),
            new CodexCliDiffAnalysisProvider(codex, heuristic),
            heuristic,
            codex);
    }
}
