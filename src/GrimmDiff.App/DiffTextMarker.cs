namespace GrimmDiff.App;

public sealed record DiffTextMarker(int Offset, int Length, DiffHighlightKind Kind);
