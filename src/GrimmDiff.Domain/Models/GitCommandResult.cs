namespace GrimmDiff.Domain;

public sealed record GitCommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool IsSuccess => ExitCode == 0;
    public string ErrorMessage => string.IsNullOrWhiteSpace(StandardError) ? StandardOutput : StandardError;
}