using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GrimmDiff.App;

public static class InputDialog
{
    public static string? Ask(string title, string prompt)
    {
        var input = new TextBox { MinWidth = 360, Margin = new Thickness(0, 12, 0, 16) };
        var dialog = new Window
        {
            Title = title, Width = 520, SizeToContent = SizeToContent.Height, MinHeight = 210,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = System.Windows.Application.Current.MainWindow,
            Background = System.Windows.Application.Current.Resources["Background"] as Brush,
            Foreground = System.Windows.Application.Current.Resources["Primary"] as Brush
        };
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = prompt, TextWrapping = TextWrapping.Wrap, MaxWidth = 470 });
        panel.Children.Add(input);
        var ok = new Button
            { Content = "OK", IsDefault = true, Width = 72, HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (_, _) => dialog.DialogResult = true;
        panel.Children.Add(ok);
        dialog.Content = panel;
        return dialog.ShowDialog() == true ? input.Text.Trim() : null;
    }
}