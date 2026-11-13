using GrimmDiff.Logic.Abstracts;
using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace GrimmDiff.Gui;

public sealed class DiffViewerControl : TextEditor
{
    private const int MaximumCachedDocuments = 8;

    public static readonly DependencyProperty RenderDocumentProperty = DependencyProperty.Register(
        nameof(RenderDocument), typeof(object), typeof(DiffViewerControl),
        new PropertyMetadata(null, OnRenderDocumentChanged));

    private readonly DiffColorizerRenderer _colorizer = new();
    private readonly Queue<DiffRenderDocumentModel> _documentOrder = [];
    private readonly Dictionary<DiffRenderDocumentModel, TextDocument> _documents = new(ReferenceEqualityComparer.Instance);
    private readonly DiffLineBackgroundRenderer _lineBackgroundRenderer = new();
    private readonly DiffLineNumberMarginControl _lineNumberMargin;

    public DiffViewerControl()
    {
        _lineNumberMargin = new DiffLineNumberMarginControl(this);
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
        TextArea.LeftMargins.Insert(0, _lineNumberMargin);
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
        ((DiffViewerControl)dependencyObject).ApplyDocument(args.NewValue as DiffRenderDocumentModel);
    }

    private void ApplyDocument(DiffRenderDocumentModel? renderDocument)
    {
        if (renderDocument is null)
        {
            Document = new TextDocument();
            _colorizer.SetMarkers([]);
            _lineBackgroundRenderer.SetMarkers([]);
            _lineNumberMargin.SetLineNumbers([]);
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
        _lineNumberMargin.SetLineNumbers(renderDocument.LineNumbers);
        TextArea.TextView.Redraw();
    }

    #endregion
}
