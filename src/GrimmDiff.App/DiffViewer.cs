using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace GrimmDiff.App;

public sealed class DiffViewer : TextEditor
{
    private const int MaximumCachedDocuments = 8;

    public static readonly DependencyProperty RenderDocumentProperty = DependencyProperty.Register(
        nameof(RenderDocument), typeof(object), typeof(DiffViewer),
        new PropertyMetadata(null, OnRenderDocumentChanged));

    private readonly DiffColorizer _colorizer = new();
    private readonly Queue<DiffRenderDocument> _documentOrder = [];
    private readonly Dictionary<DiffRenderDocument, TextDocument> _documents = new(ReferenceEqualityComparer.Instance);
    private readonly DiffLineBackgroundRenderer _lineBackgroundRenderer = new();

    public DiffViewer()
    {
        IsReadOnly = true;
        ShowLineNumbers = false;
        WordWrap = false;
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        Options.EnableEmailHyperlinks = false;
        Options.EnableHyperlinks = false;
        Options.EnableTextDragDrop = false;
        TextArea.TextView.LineTransformers.Add(_colorizer);
        TextArea.TextView.BackgroundRenderers.Add(_lineBackgroundRenderer);
    }

    public object? RenderDocument
    {
        get => GetValue(RenderDocumentProperty);
        set => SetValue(RenderDocumentProperty, value);
    }

    #region Private

    private static void OnRenderDocumentChanged(DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs args)
    {
        ((DiffViewer)dependencyObject).ApplyDocument(args.NewValue as DiffRenderDocument);
    }

    private void ApplyDocument(DiffRenderDocument? renderDocument)
    {
        if (renderDocument is null)
        {
            Document = new TextDocument();
            _colorizer.SetMarkers([]);
            _lineBackgroundRenderer.SetMarkers([]);
            return;
        }

        if (!_documents.TryGetValue(renderDocument, out var document))
        {
            document = new TextDocument(renderDocument.Text);
            _documents.Add(renderDocument, document);
            _documentOrder.Enqueue(renderDocument);
            while (_documentOrder.Count > MaximumCachedDocuments)
            {
                var stale = _documentOrder.Dequeue();
                _documents.Remove(stale);
            }
        }

        Document = document;
        _colorizer.SetMarkers(renderDocument.Markers);
        _lineBackgroundRenderer.SetMarkers(renderDocument.Markers);
        TextArea.TextView.Redraw();
    }

    #endregion
}

internal sealed class DiffColorizer : DocumentColorizingTransformer
{
    private IReadOnlyList<DiffTextMarker> _markers = [];

    public void SetMarkers(IReadOnlyList<DiffTextMarker> markers)
    {
        _markers = markers.OrderBy(marker => marker.Offset).ToList();
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        var lineEnd = line.EndOffset;
        foreach (var marker in _markers)
        {
            if (marker.Offset >= lineEnd) break;
            var markerEnd = marker.Offset + marker.Length;
            if (markerEnd <= line.Offset) continue;
            var start = Math.Max(marker.Offset, line.Offset);
            var end = Math.Min(markerEnd, lineEnd);
            if (end <= start) continue;
            ChangeLinePart(start, end, element => Apply(element, marker.Kind));
        }
    }

    #region Private

    private static void Apply(VisualLineElement element, DiffHighlightKind kind)
    {
        var (foregroundKey, backgroundKey, opacity) = kind switch
        {
            DiffHighlightKind.AddedLine => ("DiffAdded", "Background", (byte)0),
            DiffHighlightKind.RemovedLine => ("DiffRemoved", "Background", (byte)0),
            DiffHighlightKind.AddedWord => ("Primary", "DiffAdded", (byte)118),
            DiffHighlightKind.RemovedWord => ("Primary", "DiffRemoved", (byte)118),
            DiffHighlightKind.Hunk => ("DiffHunk", "Background", (byte)0),
            _ => ("Primary", "Background", (byte)0)
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

    #endregion
}

internal sealed class DiffLineBackgroundRenderer : IBackgroundRenderer
{
    private IReadOnlyList<DiffTextMarker> _markers = [];
    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (!textView.VisualLinesValid || textView.VisualLines.Count == 0) return;
        var firstOffset = textView.VisualLines.First().FirstDocumentLine.Offset;
        var lastOffset = textView.VisualLines.Last().LastDocumentLine.EndOffset;
        foreach (var marker in _markers)
        {
            if (marker.Offset > lastOffset) break;
            if (marker.Offset + marker.Length < firstOffset) continue;
            var segment = new TextSegment { StartOffset = marker.Offset, Length = marker.Length };
            var geometryBuilder = new BackgroundGeometryBuilder
                { AlignToWholePixels = true, ExtendToFullWidthAtLineEnd = true };
            geometryBuilder.AddSegment(textView, segment);
            var geometry = geometryBuilder.CreateGeometry();
            if (geometry is not null) drawingContext.DrawGeometry(BackgroundBrush(marker.Kind), null, geometry);
        }
    }

    public void SetMarkers(IReadOnlyList<DiffTextMarker> markers)
    {
        _markers = markers.Where(marker => marker.Kind is DiffHighlightKind.AddedLine or DiffHighlightKind.RemovedLine)
            .OrderBy(marker => marker.Offset).ToList();
    }

    private static Brush BackgroundBrush(DiffHighlightKind kind)
    {
        var key = kind == DiffHighlightKind.AddedLine ? "DiffAdded" : "DiffRemoved";
        var source = System.Windows.Application.Current.Resources[key] as SolidColorBrush ?? Brushes.Transparent;
        var color = source.Color;
        color.A = 36;
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}