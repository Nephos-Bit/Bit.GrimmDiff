using GrimmDiff.Logic.Abstracts;
namespace GrimmDiff.Gui;

public sealed record DiffTextMarkerModel(int Offset, int Length, DiffHighlightKindModel Kind);