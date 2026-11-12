namespace GrimmDiff.Domain;

public sealed record DiffHunk(
    string FilePath,
    DiffScope Scope,
    string Header,
    string Content,
    int OldStart,
    int NewStart);