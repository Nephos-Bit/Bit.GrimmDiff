namespace GrimmDiff.Domain;

public sealed record GitFileChange(string Path, ChangeKind Kind, DiffScope Scope);
