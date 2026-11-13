namespace GrimmDiff.Logic.Abstracts;

public sealed record GitCommandResultModel(int ExitCode, string StandardOutput, string StandardError)
{
    public bool IsSuccess => ExitCode == 0;
    public string ErrorMessage => string.IsNullOrWhiteSpace(StandardError) ? StandardOutput : StandardError;
}