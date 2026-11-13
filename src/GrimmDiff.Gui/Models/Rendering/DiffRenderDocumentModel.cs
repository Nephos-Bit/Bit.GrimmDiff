using GrimmDiff.Logic.Abstracts;
namespace GrimmDiff.Gui;

public sealed class DiffRenderDocumentModel(string text, IReadOnlyList<DiffTextMarkerModel> markers,
    IReadOnlyList<DiffLineNumbersModel> lineNumbers, DiffPageCursorModel? nextPage)
{
    public string Text { get; } = text;
    public IReadOnlyList<DiffTextMarkerModel> Markers { get; } = markers;
    public IReadOnlyList<DiffLineNumbersModel> LineNumbers { get; } = lineNumbers;
    public DiffPageCursorModel? NextPage { get; } = nextPage;

    public static DiffRenderDocumentModel FromPage(DiffRenderPageModel page)
    {
        return new DiffRenderDocumentModel(page.Text, page.Markers, page.LineNumbers, page.NextPage);
    }

    public DiffRenderDocumentModel Append(DiffRenderPageModel page)
    {
        var offset = Text.Length;
        var allMarkers = new List<DiffTextMarkerModel>(Markers.Count + page.Markers.Count);
        allMarkers.AddRange(Markers);
        allMarkers.AddRange(page.Markers.Select(marker => marker with { Offset = marker.Offset + offset }));
        var allLineNumbers = new List<DiffLineNumbersModel>(LineNumbers.Count + page.LineNumbers.Count);
        allLineNumbers.AddRange(LineNumbers);
        allLineNumbers.AddRange(page.LineNumbers);
        return new DiffRenderDocumentModel(Text + page.Text, allMarkers, allLineNumbers, page.NextPage);
    }
}
