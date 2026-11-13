using GrimmDiff.Logic.Abstracts;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace GrimmDiff.Gui;

internal sealed class DiffLineBackgroundRenderer : IBackgroundRenderer
{
    private IReadOnlyList<DiffTextMarkerModel> _markers = [];
    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!textView.VisualLinesValid || textView.VisualLines.Count == 0) return;
        var first = textView.VisualLines.First().FirstDocumentLine.Offset;
        var last = textView.VisualLines.Last().LastDocumentLine.EndOffset;
        foreach (var marker in _markers)
        {
            if (marker.Offset > last) break;
            if (marker.Offset + marker.Length < first) continue;
            var builder = new BackgroundGeometryBuilder
                { AlignToWholePixels = true, ExtendToFullWidthAtLineEnd = true };
            builder.AddSegment(textView, new TextSegment { StartOffset = marker.Offset, Length = marker.Length });
            if (builder.CreateGeometry() is { } geometry)
                drawingContext.DrawGeometry(BackgroundBrush(marker.Kind), null, geometry);
        }
    }

    public void SetMarkers(IReadOnlyList<DiffTextMarkerModel> markers)
    {
        _markers = markers.Where(marker => marker.Kind is DiffHighlightKindModel.AddedLine or DiffHighlightKindModel.RemovedLine)
            .OrderBy(marker => marker.Offset).ToList();
    }

    private static Brush BackgroundBrush(DiffHighlightKindModel kind)
    {
        var key = kind == DiffHighlightKindModel.AddedLine ? "DiffAdded" : "DiffRemoved";
        var source = System.Windows.Application.Current.Resources[key] as SolidColorBrush ?? Brushes.Transparent;
        var color = source.Color;
        color.A = 36;
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}