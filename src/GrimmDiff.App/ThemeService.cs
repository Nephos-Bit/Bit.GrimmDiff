using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace GrimmDiff.App;

public static class ThemeService
{
    public static void Apply(string theme)
    {
        var light = theme == "Light" || (theme == "System" && Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 0) is int value && value != 0);
        Set("Background", light ? "#F6F6F6" : "#080808"); Set("Surface", light ? "#FFFFFF" : "#101010"); Set("Primary", light ? "#171717" : "#ECECEC");
        Set("Secondary", light ? "#606060" : "#A0A0A0"); Set("Border", light ? "#B8B8B8" : "#303030"); Set("Active", light ? "#007A29" : "#00B83A"); Set("Critical", light ? "#B72C08" : "#FF3B0A");
    }
    private static void Set(string key, string color) => Application.Current.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
}
