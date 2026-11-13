namespace GrimmDiff.Logic.Abstracts;

public interface ICodexStructuredRunnerPort : ICodexSessionClientPort
{
    Task<string> RunStructuredAsync(string prompt, string schema, TimeSpan timeout,
        CancellationToken cancellationToken = default);
}