using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GrimmDiff.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = AppCompositionRoot.CreateMainViewModel();
        DataContext = _viewModel;
        AddApplicationVersion();
        ContentRendered += OnContentRendered;
        Closed += (_, _) => _viewModel.Dispose();
    }

    #region Private

    private async void OnContentRendered(object? sender, EventArgs e)
    {
        ContentRendered -= OnContentRendered;
        await _viewModel.InitializeAsync();
        await _viewModel.CheckForUpdatesOnStartupAsync();
    }

    private void BranchButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (BranchButton.ContextMenu is null || !BranchButton.IsEnabled) return;
        BranchButton.ContextMenu.PlacementTarget = BranchButton;
        BranchButton.ContextMenu.IsOpen = true;
    }

    private void AddApplicationVersion()
    {
        var root = (Grid)Content;
        var header = (Grid)root.Children[0];
        var logo = header.Children.OfType<TextBlock>().Single(textBlock => textBlock.Text == "grimmdiff");
        var version = new TextBlock
        {
            FontSize = 12,
            Opacity = 0.5,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        version.SetBinding(TextBlock.TextProperty, new Binding(nameof(MainViewModel.ApplicationVersion))
        {
            StringFormat = "v{0}"
        });

        void PositionVersion()
        {
            version.Margin = new Thickness(logo.ActualWidth + 8, 0, 0, 0);
        }

        logo.SizeChanged += (_, _) => PositionVersion();
        PositionVersion();
        header.Children.Add(version);
    }

    #endregion
}