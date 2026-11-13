using GrimmDiff.Logic.Abstracts;
namespace GrimmDiff.Gui;

public sealed record JiraCommitSubjectPartModel(string Text, string Url) : CommitSubjectPartModel(Text);