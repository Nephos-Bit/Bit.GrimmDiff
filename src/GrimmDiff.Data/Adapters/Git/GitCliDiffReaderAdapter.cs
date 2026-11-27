using GrimmDiff.Data.Services;
using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.Abstracts.Ports;


namespace GrimmDiff.Data.Adapters.Git;

public sealed class GitCliDiffReaderAdapter : IRepositoryDiffReaderPort
{
    public async Task<string> GetDiffAsync(string repositoryPath, DiffScopeModel scope, string? path = null,
        CancellationToken cancellationToken = default)
    {
        var arguments = scope == DiffScopeModel.Staged
            ? new List<string>
                { "-c", "core.fsmonitor=false", "-c", "core.quotePath=false", "diff", "--cached", "--no-ext-diff", "--no-textconv", "--unified=3" }
            : ["-c", "core.fsmonitor=false", "-c", "core.quotePath=false", "diff", "--no-ext-diff", "--no-textconv", "--unified=3"];
        if (!string.IsNullOrWhiteSpace(path))
        {
            arguments.Add("--");
            arguments.Add(path);
        }
        var diff = (await GitCliProcessRunner.RunAsync(repositoryPath, arguments, cancellationToken)).StandardOutput;
        return scope == DiffScopeModel.WorkingTree && string.IsNullOrWhiteSpace(path)
            ? diff + await GetUntrackedDiffAsync(repositoryPath, cancellationToken)
            : diff;
    }

    public async Task<string> GetCommitDiffAsync(string repositoryPath, string commitId, string? path = null,
        CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>
        {
            "-c", "core.fsmonitor=false", "-c", "core.quotePath=false", "show", "--format=", "--no-ext-diff", "--no-textconv", "--unified=3", commitId
        };
        if (!string.IsNullOrWhiteSpace(path))
        {
            arguments.Add("--");
            arguments.Add(path);
        }
        return (await GitCliProcessRunner.RunAsync(repositoryPath, arguments, cancellationToken)).StandardOutput;
    }

    private static async Task<string> GetUntrackedDiffAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        var files = await GitCliProcessRunner.RunAsync(repositoryPath, ["ls-files", "--others", "--exclude-standard", "-z"],
            cancellationToken);
        var paths = files.StandardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        var diffs = new List<string>();
        foreach (var file in paths)
        {
            var diff = await GitCliProcessRunner.RunAsync(repositoryPath,
                ["-c", "core.quotePath=false", "diff", "--no-index", "--no-ext-diff", "--unified=3", "--", "/dev/null", file], cancellationToken);
            if (!string.IsNullOrWhiteSpace(diff.StandardOutput)) diffs.Add(diff.StandardOutput);
        }
        return diffs.Count == 0 ? string.Empty : "\n" + string.Join("\n", diffs);
    }
}
