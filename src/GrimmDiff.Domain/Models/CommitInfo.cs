namespace GrimmDiff.Domain;

public sealed record CommitInfo(string Id, string Subject, string Author, DateTimeOffset Date);