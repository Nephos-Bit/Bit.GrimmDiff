using System.Windows.Media;

namespace GrimmDiff.Gui;

public static class MascotCatalog
{
    public const string DefaultId = "moss";

    public static IReadOnlyList<MascotOption> Options { get; } =
    [
        Create("moss", "Moss", "#0E1711", "#27372C", "#739C7D", "#B2CEB8"),
        Create("ember", "Ember", "#3B1D18", "#C95232", "#F58B45", "#FFE0A8"),
        Create("luma", "Luma", "#233849", "#5585AA", "#A4CBE0", "#F4F9E8"),
        Create("bolt", "Bolt", "#29203F", "#795BAE", "#D080C9", "#80F0E7"),
        Create("sprig", "Sprig", "#23402B", "#418B57", "#9FD16D", "#E7EDAD"),
        Create("wisp", "Wisp", "#39304E", "#A798D3", "#D3C4EF", "#FFF5FA"),
        Create("sgt-schnabel", "Sgt. Schnabel", "#69471F", "#F4CA3D", "#E98226", "#FFF0A3"),
        Create("berta", "Berta", "#493B32", "#FFFDF3", "#D93B2A", "#FFFFFF", "#ECA348")
    ];

    public static MascotOption Find(string? id) =>
        Options.FirstOrDefault(option => option.Id == id) ?? Options[0];

    private static MascotOption Create(string id, string name, string outline, string body, string accent, string highlight,
        string? detail = null) =>
        new(id, name, Brush(outline), Brush(body), Brush(accent), Brush(highlight), detail is null ? null : Brush(detail));

    private static Brush Brush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }
}
