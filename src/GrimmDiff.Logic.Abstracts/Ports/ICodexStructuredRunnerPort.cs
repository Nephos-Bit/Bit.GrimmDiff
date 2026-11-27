namespace GrimmDiff.Logic.Abstracts.Ports;

public interface ICodexStructuredRunnerPort
{
    Task<string> RunStructuredAsync(string prompt, string schema, TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
