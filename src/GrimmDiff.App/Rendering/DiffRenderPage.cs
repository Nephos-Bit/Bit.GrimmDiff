namespace GrimmDiff.App;

public sealed record DiffRenderPage(string Text, IReadOnlyList<DiffTextMarker> Markers, DiffPageCursor? NextPage);