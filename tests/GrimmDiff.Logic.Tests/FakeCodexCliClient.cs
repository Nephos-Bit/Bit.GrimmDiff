using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.Abstracts.Ports;

namespace GrimmDiff.Logic.Tests;

internal sealed class FakeCodexCliClient(IEnumerable<string> responses) : ICodexSessionClientPort, ICodexStructuredRunnerPort
{
    private readonly Queue<string> _responses = new(responses);
    public List<string> Prompts { get; } = [];
    public List<string> Schemas { get; } = [];

    public Task<CodexCliStatusModel> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CodexCliStatusModel(CodexCliStatusKindModel.Ready, "Ready"));
    }

    public Task<CodexCliStatusModel> LoginAsync(CancellationToken cancellationToken = default)
    {
        return GetStatusAsync(cancellationToken);
    }

    public Task<CodexAccountUsageModel> GetAccountUsageAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new CodexAccountUsageModel(null, null));
    }

    public Task<string> RunStructuredAsync(string prompt, string schema, TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        Prompts.Add(prompt);
        Schemas.Add(schema);
        return Task.FromResult(_responses.Dequeue());
    }
}