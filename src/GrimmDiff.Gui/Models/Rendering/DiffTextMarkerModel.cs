namespace GrimmDiff.Gui.Models.Rendering;

public sealed record DiffTextMarkerModel(int Offset, int Length, DiffHighlightKindModel Kind);