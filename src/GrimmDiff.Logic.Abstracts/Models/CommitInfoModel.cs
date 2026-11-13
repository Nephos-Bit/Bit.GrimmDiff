namespace GrimmDiff.Logic.Abstracts;

public sealed record CommitInfoModel(string Id, string Subject, string Author, DateTimeOffset Date);