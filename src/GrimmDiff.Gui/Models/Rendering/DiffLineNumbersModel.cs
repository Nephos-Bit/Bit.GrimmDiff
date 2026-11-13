using GrimmDiff.Logic.Abstracts;
namespace GrimmDiff.Gui;

public sealed record DiffLineNumbersModel(int? OldLine, int? NewLine);
