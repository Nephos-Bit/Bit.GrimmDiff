using GrimmDiff.Logic.Abstracts;
namespace GrimmDiff.Gui;

public sealed record PlainCommitSubjectPartModel(string Text) : CommitSubjectPartModel(Text);