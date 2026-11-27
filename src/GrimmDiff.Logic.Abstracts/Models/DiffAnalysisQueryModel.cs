namespace GrimmDiff.Logic.Abstracts.Models;

public sealed record DiffAnalysisQueryModel(string RepositoryPath, string? CommitId, string Language, bool UseAi);