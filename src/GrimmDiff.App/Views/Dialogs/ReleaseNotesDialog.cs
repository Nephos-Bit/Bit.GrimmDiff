using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GrimmDiff.App;

public static class ReleaseNotesDialog
{
    public static void Show(string title, string notes)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 640,
            MinWidth = 440,
            MaxHeight = 640,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = System.Windows.Application.Current.MainWindow,
            Background = System.Windows.Application.Current.Resources["Background"] as Brush,
            Foreground = System.Windows.Application.Current.Resources["Primary"] as Brush,
            ResizeMode = ResizeMode.CanResize,
            ShowInTaskbar = false
        };

        var root = new Grid { Margin = new Thickness(24) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var heading = new TextBlock
        {
            Text = title.ToUpperInvariant(),
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 16)
        };
        root.Children.Add(heading);

        var content = new TextBlock
        {
            Text = notes,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 16,
            LineHeight = 24,
            Margin = new Thickness(0, 0, 0, 24)
        };
        var scrollViewer = new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MaxHeight = 460
        };
        Grid.SetRow(scrollViewer, 1);
        root.Children.Add(scrollViewer);

        var close = new Button
        {
            Content = "OK",
            IsDefault = true,
            MinWidth = 72,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0)
        };
        close.Click += (_, _) => dialog.Close();
        Grid.SetRow(close, 2);
        root.Children.Add(close);

        dialog.Content = root;
        dialog.ShowDialog();
    }
}
