namespace GrimmDiff.Logic.Abstracts.Models;

public sealed record DiffAnalysisRequestModel(string RepositoryPath, DiffScopeModel Scope, string Diff, string Language);