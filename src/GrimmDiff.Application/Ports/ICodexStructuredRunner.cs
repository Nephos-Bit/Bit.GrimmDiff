namespace GrimmDiff.Application;
public interface ICodexStructuredRunner : ICodexSessionClient { Task<string> RunStructuredAsync(string prompt, string schema, TimeSpan timeout, CancellationToken cancellationToken = default); }
