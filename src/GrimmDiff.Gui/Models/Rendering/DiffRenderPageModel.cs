using GrimmDiff.Logic.Abstracts;
namespace GrimmDiff.Gui;

public sealed record DiffRenderPageModel(string Text, IReadOnlyList<DiffTextMarkerModel> Markers,
    IReadOnlyList<DiffLineNumbersModel> LineNumbers, DiffPageCursorModel? NextPage);
