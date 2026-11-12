namespace GrimmDiff.App;

public sealed record PlainCommitSubjectPart(string Text) : CommitSubjectPart(Text);