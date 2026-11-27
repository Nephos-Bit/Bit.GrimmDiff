using System.ComponentModel;
using GrimmDiff.Data.Abstracts.Ports;
using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.Abstracts.Ports;

namespace GrimmDiff.Data.Adapters.Codex;

public sealed class CodexCliClientAdapter(ICodexProcessRunnerPort? processRunner = null) : ICodexSessionClientPort
{
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan LoginTimeout = TimeSpan.FromMinutes(5);

    private readonly ICodexProcessRunnerPort _processRunner = processRunner ?? new SystemCodexProcessRunnerAdapter();
    private readonly bool _resolveSystemExecutable = processRunner is null;

    public async Task<CodexCliStatusModel> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var executable = ResolveExecutable();
        try
        {
            var result = await _processRunner.RunAsync(CodexCliProcessConfiguration.Request(executable, ["login", "status"], null, StatusTimeout),
                cancellationToken);
            if (result.TimedOut)
                return new CodexCliStatusModel(CodexCliStatusKindModel.Unavailable, "The Codex CLI status check timed out.");
            var status = $"{result.StandardOutput}\n{result.StandardError}";
            if (status.Contains("not logged in", StringComparison.OrdinalIgnoreCase))
                return new CodexCliStatusModel(CodexCliStatusKindModel.NotAuthenticated, "Codex is not signed in.");
            if (status.Contains("api key", StringComparison.OrdinalIgnoreCase) ||
                status.Contains("apikey", StringComparison.OrdinalIgnoreCase))
                return new CodexCliStatusModel(CodexCliStatusKindModel.ApiKeyAuthentication,
                    "Codex is signed in with an API key instead of ChatGPT.");
            if (result.ExitCode == 0 && status.Contains("ChatGPT", StringComparison.OrdinalIgnoreCase))
                return new CodexCliStatusModel(CodexCliStatusKindModel.Ready, "ChatGPT sign-in is ready.");
            if (result.ExitCode == 0)
                return new CodexCliStatusModel(CodexCliStatusKindModel.UnsupportedAuthentication,
                    "The active Codex authentication is not a ChatGPT sign-in.");
            return new CodexCliStatusModel(CodexCliStatusKindModel.NotAuthenticated, "Codex is not signed in with ChatGPT.");
        }
        catch (Win32Exception)
        {
            return new CodexCliStatusModel(CodexCliStatusKindModel.Missing, "The Codex CLI was not found.");
        }
        catch (FileNotFoundException)
        {
            return new CodexCliStatusModel(CodexCliStatusKindModel.Missing, "The Codex CLI was not found.");
        }
    }

    public async Task<CodexCliStatusModel> LoginAsync(CancellationToken cancellationToken = default)
    {
        var executable = ResolveExecutable();
        try
        {
            var result =
                await _processRunner.RunAsync(CodexCliProcessConfiguration.Request(executable, ["login"], null, LoginTimeout), cancellationToken);
            if (result.TimedOut)
                return new CodexCliStatusModel(CodexCliStatusKindModel.Unavailable, "The ChatGPT sign-in timed out.");
            if (result.ExitCode != 0)
                return new CodexCliStatusModel(CodexCliStatusKindModel.Unavailable, "ChatGPT sign-in did not complete.");
            return await GetStatusAsync(cancellationToken);
        }
        catch (Win32Exception)
        {
            return new CodexCliStatusModel(CodexCliStatusKindModel.Missing, "The Codex CLI was not found.");
        }
        catch (FileNotFoundException)
        {
            return new CodexCliStatusModel(CodexCliStatusKindModel.Missing, "The Codex CLI was not found.");
        }
    }

    public Task<CodexAccountUsageModel> GetAccountUsageAsync(CancellationToken cancellationToken = default)
    {
        return CodexAccountUsageReader.GetAsync(ResolveExecutable(), CodexCliProcessConfiguration.RemovedCredentialVariables, cancellationToken);
    }
    private string ResolveExecutable() => CodexCliProcessConfiguration.ResolveExecutable(_resolveSystemExecutable);

}
