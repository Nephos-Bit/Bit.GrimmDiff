using System.Windows;
namespace GrimmDiff.App;
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    public MainWindow()
    {
        InitializeComponent();
        _viewModel = AppCompositionRoot.CreateMainViewModel();
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
