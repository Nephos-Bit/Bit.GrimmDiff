using System.Windows;
using GrimmDiff.Gui.ViewModels;

namespace GrimmDiff.Gui.Views;

public partial class MainView : Window
{
    public MainView(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
