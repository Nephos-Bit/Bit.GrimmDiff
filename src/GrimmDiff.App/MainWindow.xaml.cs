using System.Windows;
using GrimmDiff.Infrastructure;
namespace GrimmDiff.App;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(new GitCliRepositoryService(), new LocalHeuristicDiffAnalysisProvider());
    }
}
