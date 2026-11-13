namespace GrimmDiff.Logic.Abstracts;

public sealed record DiffHunkModel(
    string FilePath,
    DiffScopeModel Scope,
    string Header,
    string Content,
    int OldStart,
    int NewStart);