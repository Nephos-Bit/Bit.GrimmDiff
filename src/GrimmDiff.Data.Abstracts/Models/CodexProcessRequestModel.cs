namespace GrimmDiff.Data.Abstracts;

public sealed record CodexProcessRequestModel(
    string Executable,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    string? StandardInput,
    TimeSpan Timeout,
    IReadOnlyList<string> EnvironmentVariablesToRemove);
