using GrimmDiff.Data.Abstracts.Models;

namespace GrimmDiff.Data.Abstracts.Ports;

public interface ICodexProcessRunnerPort
{
    Task<CodexProcessResultModel> RunAsync(CodexProcessRequestModel request,
        CancellationToken cancellationToken = default);
}
