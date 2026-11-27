namespace GrimmDiff.Gui.Models.Rendering;

public sealed record DiffRenderPageModel(string Text, IReadOnlyList<DiffTextMarkerModel> Markers,
    IReadOnlyList<DiffLineNumbersModel> LineNumbers, DiffPageCursorModel? NextPage);
