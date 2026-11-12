namespace GrimmDiff.Domain;

public sealed record DiffAnalysisRequest(string RepositoryPath, DiffScope Scope, string Diff, string Language);