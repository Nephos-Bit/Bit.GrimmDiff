namespace GrimmDiff.Application;
public sealed record DiffAnalysisQuery(string RepositoryPath, string? CommitId, string Language, bool UseAi);
