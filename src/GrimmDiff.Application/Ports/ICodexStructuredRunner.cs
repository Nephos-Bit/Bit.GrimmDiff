namespace GrimmDiff.Application;
public interface ICodexStructuredRunner : ICodexSessionClient { Task<string> RunStructuredAsync(string repositoryPath, string prompt, string schema, TimeSpan timeout, CancellationToken cancellationToken = default); }
