namespace GrimmDiff.Logic.Abstracts.Models;

public sealed record CommitInfoModel(string Id, string Subject, string Author, DateTimeOffset Date);