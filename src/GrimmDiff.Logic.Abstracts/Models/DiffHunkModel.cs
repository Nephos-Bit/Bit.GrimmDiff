namespace GrimmDiff.Logic.Abstracts.Models;

public sealed record DiffHunkModel(
    string FilePath,
    DiffScopeModel Scope,
    string Header,
    string Content,
    int OldStart,
    int NewStart);