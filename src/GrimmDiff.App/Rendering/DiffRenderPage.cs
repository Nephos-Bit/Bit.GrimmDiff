namespace GrimmDiff.App;

public sealed record DiffRenderPage(string Text, IReadOnlyList<DiffTextMarker> Markers,
    IReadOnlyList<DiffLineNumbers> LineNumbers, DiffPageCursor? NextPage);
