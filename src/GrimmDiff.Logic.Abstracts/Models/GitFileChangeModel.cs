namespace GrimmDiff.Logic.Abstracts;

public sealed record GitFileChangeModel(string Path, ChangeKindModel Kind, DiffScopeModel Scope);