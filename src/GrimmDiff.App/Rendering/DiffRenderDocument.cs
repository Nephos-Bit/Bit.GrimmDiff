namespace GrimmDiff.App;

public sealed class DiffRenderDocument(string text, IReadOnlyList<DiffTextMarker> markers, DiffPageCursor? nextPage)
{
    public string Text { get; } = text;
    public IReadOnlyList<DiffTextMarker> Markers { get; } = markers;
    public DiffPageCursor? NextPage { get; } = nextPage;

    public static DiffRenderDocument FromPage(DiffRenderPage page) => new(page.Text, page.Markers, page.NextPage);

    public DiffRenderDocument Append(DiffRenderPage page)
    {
        var offset = Text.Length;
        var allMarkers = new List<DiffTextMarker>(Markers.Count + page.Markers.Count);
        allMarkers.AddRange(Markers);
        allMarkers.AddRange(page.Markers.Select(marker => marker with { Offset = marker.Offset + offset }));
        return new DiffRenderDocument(Text + page.Text, allMarkers, page.NextPage);
    }
}
