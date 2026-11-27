namespace GrimmDiff.Data.Abstracts.Models;

public sealed record CodexProcessResultModel(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut = false);
