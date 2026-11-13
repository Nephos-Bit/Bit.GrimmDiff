using GrimmDiff.Data.Abstracts;

namespace GrimmDiff.Data.Abstracts;

public interface ICodexProcessRunnerPort
{
    Task<CodexProcessResultModel> RunAsync(CodexProcessRequestModel request,
        CancellationToken cancellationToken = default);
}
