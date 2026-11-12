using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace GrimmDiff.App;

internal sealed class DiffColorizer : DocumentColorizingTransformer
{
    private IReadOnlyList<DiffTextMarker> _markers = [];

    public void SetMarkers(IReadOnlyList<DiffTextMarker> markers)
    {
        _markers = markers.OrderBy(marker => marker.Offset).ToList();
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        foreach (var marker in _markers)
        {
            if (marker.Offset >= line.EndOffset) break;
            var start = Math.Max(marker.Offset, line.Offset);
            var end = Math.Min(marker.Offset + marker.Length, line.EndOffset);
            if (end > start) ChangeLinePart(start, end, element => Apply(element, marker.Kind));
        }
    }

    private static void Apply(VisualLineElement element, DiffHighlightKind kind)
    {
        var (foregroundKey, backgroundKey, opacity) = kind switch
        {
            DiffHighlightKind.AddedLine => ("DiffAdded", "Background", (byte)0),
            DiffHighlightKind.RemovedLine => ("DiffRemoved", "Background", (byte)0),
            DiffHighlightKind.AddedWord => ("Primary", "DiffAdded", (byte)118),
            DiffHighlightKind.RemovedWord => ("Primary", "DiffRemoved", (byte)118),
            DiffHighlightKind.Hunk => ("DiffHunk", "Background", (byte)0), _ => ("Primary", "Background", (byte)0)
        };
        element.TextRunProperties.SetForegroundBrush(Brush(foregroundKey, 255));
        if (opacity > 0) element.TextRunProperties.SetBackgroundBrush(Brush(backgroundKey, opacity));
    }

    private static Brush Brush(string resourceKey, byte alpha)
    {
        var source = System.Windows.Application.Current.Resources[resourceKey] as SolidColorBrush ??
                     Brushes.Transparent;
        var color = source.Color;
        color.A = alpha;
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}