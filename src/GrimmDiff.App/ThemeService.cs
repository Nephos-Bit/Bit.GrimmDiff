using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace GrimmDiff.App;

public static class ThemeService
{
    public static void Apply(string theme)
    {
        var light = theme == "Light" || (theme == "System" && Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 0) is int value && value != 0);
        Set("Background", light ? "#F6F6F6" : "#080808"); Set("Surface", light ? "#FFFFFF" : "#101010"); Set("SurfaceRaised", light ? "#EBEBEB" : "#141414"); Set("Primary", light ? "#171717" : "#ECECEC");
        Set("Secondary", light ? "#606060" : "#A0A0A0"); Set("Border", light ? "#B8B8B8" : "#303030"); Set("Active", light ? "#007A29" : "#00B83A"); Set("Warning", light ? "#7A5700" : "#C99526"); Set("Critical", light ? "#B72C08" : "#FF3B0A");
        Set("DiffAdded", light ? "#087A29" : "#65E879"); Set("DiffRemoved", light ? "#B72C08" : "#FF7957"); Set("DiffHunk", light ? "#006EA8" : "#46B7FF");
        Set("CodeKeyword", light ? "#805A00" : "#D7A84A"); Set("CodeType", light ? "#075F98" : "#72B7E8"); Set("CodeMember", light ? "#734A92" : "#C99BE8"); Set("CodeString", light ? "#087A29" : "#75C68A"); Set("CodeComment", light ? "#707070" : "#6F6F6F");
    }
    private static void Set(string key, string color) => System.Windows.Application.Current.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
}
