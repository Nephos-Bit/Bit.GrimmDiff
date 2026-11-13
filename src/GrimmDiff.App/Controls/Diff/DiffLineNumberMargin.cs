using System.Globalization;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace GrimmDiff.App;

internal sealed class DiffLineNumberMargin : AbstractMargin
{
    private readonly TextEditor _editor;
    private IReadOnlyList<DiffLineNumbers> _lineNumbers = [];

    public DiffLineNumberMargin(TextEditor editor)
    {
        _editor = editor;
    }

    public void SetLineNumbers(IReadOnlyList<DiffLineNumbers> lineNumbers)
    {
        _lineNumbers = lineNumbers;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(76, 0);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        TextView.EnsureVisualLines();
        var brush = System.Windows.Application.Current.Resources["Secondary"] as Brush ?? Brushes.Gray;
        foreach (var visualLine in TextView.VisualLines)
        {
            var index = visualLine.FirstDocumentLine.LineNumber - 1;
            if (index < 0 || index >= _lineNumbers.Count) continue;
            var numbers = _lineNumbers[index];
            if (numbers.OldLine is null && numbers.NewLine is null) continue;

            var oldNumber = numbers.OldLine?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
            var newNumber = numbers.NewLine?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
            var text = new FormattedText($"{oldNumber,5} {newNumber,5}", CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, new Typeface(_editor.FontFamily, _editor.FontStyle, _editor.FontWeight,
                    _editor.FontStretch), _editor.FontSize, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            var y = visualLine.GetTextLineVisualYPosition(visualLine.TextLines[0], VisualYPosition.TextTop) -
                    TextView.VerticalOffset;
            drawingContext.DrawText(text, new Point(0, y));
        }
    }
}
