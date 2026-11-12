using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

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