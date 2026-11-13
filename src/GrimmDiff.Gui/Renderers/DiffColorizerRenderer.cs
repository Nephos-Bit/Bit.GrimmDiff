using GrimmDiff.Logic.Abstracts;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace GrimmDiff.Gui;

internal sealed class DiffColorizerRenderer : DocumentColorizingTransformer
{
    private IReadOnlyList<DiffTextMarkerModel> _markers = [];

    public void SetMarkers(IReadOnlyList<DiffTextMarkerModel> markers)
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

    private static void Apply(VisualLineElement element, DiffHighlightKindModel kind)
    {
        var (foregroundKey, backgroundKey, opacity) = kind switch
        {
            DiffHighlightKindModel.AddedLine => ("DiffAdded", "Background", (byte)0),
            DiffHighlightKindModel.RemovedLine => ("DiffRemoved", "Background", (byte)0),
            DiffHighlightKindModel.AddedWord => ("Primary", "DiffAdded", (byte)118),
            DiffHighlightKindModel.RemovedWord => ("Primary", "DiffRemoved", (byte)118),
            DiffHighlightKindModel.Hunk => ("DiffHunkModel", "Background", (byte)0), _ => ("Primary", "Background", (byte)0)
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