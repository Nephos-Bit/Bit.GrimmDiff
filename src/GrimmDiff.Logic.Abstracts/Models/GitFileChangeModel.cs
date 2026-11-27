namespace GrimmDiff.Logic.Abstracts.Models;

public sealed record GitFileChangeModel(string Path, ChangeKindModel Kind, DiffScopeModel Scope);