namespace GrimmDiff.Data.Abstracts;

public sealed record CodexProcessResultModel(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut = false);
