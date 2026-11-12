namespace GrimmDiff.App;

public sealed record JiraCommitSubjectPart(string Text, string Url) : CommitSubjectPart(Text);