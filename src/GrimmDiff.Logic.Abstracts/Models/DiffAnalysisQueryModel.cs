namespace GrimmDiff.Logic.Abstracts;

public sealed record DiffAnalysisQueryModel(string RepositoryPath, string? CommitId, string Language, bool UseAi);