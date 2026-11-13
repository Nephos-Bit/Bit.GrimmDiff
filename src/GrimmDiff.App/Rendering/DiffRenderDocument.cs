namespace GrimmDiff.App;

public sealed class DiffRenderDocument(string text, IReadOnlyList<DiffTextMarker> markers,
    IReadOnlyList<DiffLineNumbers> lineNumbers, DiffPageCursor? nextPage)
{
    public string Text { get; } = text;
    public IReadOnlyList<DiffTextMarker> Markers { get; } = markers;
    public IReadOnlyList<DiffLineNumbers> LineNumbers { get; } = lineNumbers;
    public DiffPageCursor? NextPage { get; } = nextPage;

    public static DiffRenderDocument FromPage(DiffRenderPage page)
    {
        return new DiffRenderDocument(page.Text, page.Markers, page.LineNumbers, page.NextPage);
    }

    public DiffRenderDocument Append(DiffRenderPage page)
    {
        var offset = Text.Length;
        var allMarkers = new List<DiffTextMarker>(Markers.Count + page.Markers.Count);
        allMarkers.AddRange(Markers);
        allMarkers.AddRange(page.Markers.Select(marker => marker with { Offset = marker.Offset + offset }));
        var allLineNumbers = new List<DiffLineNumbers>(LineNumbers.Count + page.LineNumbers.Count);
        allLineNumbers.AddRange(LineNumbers);
        allLineNumbers.AddRange(page.LineNumbers);
        return new DiffRenderDocument(Text + page.Text, allMarkers, allLineNumbers, page.NextPage);
    }
}
