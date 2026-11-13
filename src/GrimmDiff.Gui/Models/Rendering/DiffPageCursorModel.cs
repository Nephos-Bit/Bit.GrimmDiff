using GrimmDiff.Logic.Abstracts;
namespace GrimmDiff.Gui;

public sealed record DiffPageCursorModel(int HunkIndex, int ContentLineIndex);