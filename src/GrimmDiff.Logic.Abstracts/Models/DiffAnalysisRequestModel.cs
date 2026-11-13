namespace GrimmDiff.Logic.Abstracts;

public sealed record DiffAnalysisRequestModel(string RepositoryPath, DiffScopeModel Scope, string Diff, string Language);