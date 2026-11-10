using System.Diagnostics;
using System.Globalization;
using System.ComponentModel;
using GrimmDiff.Domain;

namespace GrimmDiff.Infrastructure;

public sealed class GitCliRepositoryService : IGitRepositoryService
{
    public async Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default) => (await RunAsync(null, ["--version"], cancellationToken)).IsSuccess;

    public async Task<RepositorySnapshot> LoadAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        var branch = await RunAsync(repositoryPath, ["branch", "--show-current"], cancellationToken);
        var porcelain = await RunAsync(repositoryPath, ["status", "--porcelain=v1", "-uall"], cancellationToken);
        var branches = await RunAsync(repositoryPath, ["branch", "--format=%(HEAD)|%(refname:short)"], cancellationToken);
        var history = await RunAsync(repositoryPath, ["log", "-30", "--format=%H%x1f%s%x1f%an%x1f%aI"], cancellationToken);
        var state = await GetStateAsync(repositoryPath, cancellationToken);
        return new RepositorySnapshot(repositoryPath, branch.StandardOutput.Trim(), state, ParseChanges(porcelain.StandardOutput), ParseBranches(branches.StandardOutput), ParseHistory(history.StandardOutput));
    }

    public async Task<string> GetDiffAsync(string repositoryPath, DiffScope scope, string? path = null, CancellationToken cancellationToken = default)
    {
        var arguments = scope == DiffScope.Staged ? new List<string> { "diff", "--cached", "--no-ext-diff", "--unified=3" } : ["diff", "--no-ext-diff", "--unified=3"];
        if (!string.IsNullOrWhiteSpace(path)) { arguments.Add("--"); arguments.Add(path); }
        return (await RunAsync(repositoryPath, arguments, cancellationToken)).StandardOutput;
    }

    public async Task<string> GetCommitDiffAsync(string repositoryPath, string commitId, string? path = null, CancellationToken cancellationToken = default)
    {
        var arguments = new List<string> { "show", "--format=", "--no-ext-diff", "--unified=3", commitId };
        if (!string.IsNullOrWhiteSpace(path)) { arguments.Add("--"); arguments.Add(path); }
        return (await RunAsync(repositoryPath, arguments, cancellationToken)).StandardOutput;
    }

    public Task<GitCommandResult> StageAsync(string repositoryPath, string path, CancellationToken cancellationToken = default) => RunAsync(repositoryPath, ["add", "--", path], cancellationToken);
    public Task<GitCommandResult> UnstageAsync(string repositoryPath, string path, CancellationToken cancellationToken = default) => RunAsync(repositoryPath, ["restore", "--staged", "--", path], cancellationToken);
    public Task<GitCommandResult> CommitAsync(string repositoryPath, string message, CancellationToken cancellationToken = default) => RunAsync(repositoryPath, ["commit", "-m", message], cancellationToken);
    public Task<GitCommandResult> FetchAsync(string repositoryPath, CancellationToken cancellationToken = default) => RunAsync(repositoryPath, ["fetch"], cancellationToken);
    public Task<GitCommandResult> PullAsync(string repositoryPath, CancellationToken cancellationToken = default) => RunAsync(repositoryPath, ["pull"], cancellationToken);
    public Task<GitCommandResult> PushAsync(string repositoryPath, CancellationToken cancellationToken = default) => RunAsync(repositoryPath, ["push"], cancellationToken);
    public Task<GitCommandResult> SwitchBranchAsync(string repositoryPath, string branch, CancellationToken cancellationToken = default) => RunAsync(repositoryPath, ["switch", branch], cancellationToken);
    public Task<GitCommandResult> CreateBranchAsync(string repositoryPath, string branch, CancellationToken cancellationToken = default) => RunAsync(repositoryPath, ["switch", "-c", branch], cancellationToken);
    public Task<GitCommandResult> CloneAsync(string source, string destination, CancellationToken cancellationToken = default) => RunAsync(null, ["clone", source, destination], cancellationToken);

    private static async Task<RepositoryState> GetStateAsync(string path, CancellationToken ct)
    {
        if (Directory.Exists(Path.Combine(path, ".git", "rebase-merge")) || Directory.Exists(Path.Combine(path, ".git", "rebase-apply"))) return RepositoryState.RebaseInProgress;
        if (File.Exists(Path.Combine(path, ".git", "MERGE_HEAD"))) return RepositoryState.MergeInProgress;
        if (File.Exists(Path.Combine(path, ".git", "CHERRY_PICK_HEAD"))) return RepositoryState.CherryPickInProgress;
        var status = await RunAsync(path, ["diff", "--name-only", "--diff-filter=U"], ct);
        return string.IsNullOrWhiteSpace(status.StandardOutput) ? RepositoryState.Ready : RepositoryState.Conflict;
    }

    private static IReadOnlyList<GitFileChange> ParseChanges(string text) => text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line =>
    {
        var index = Kind(line[0]); var worktree = Kind(line[1]); var path = line[3..];
        return new[] { index == null ? null : new GitFileChange(path, index.Value, DiffScope.Staged), worktree == null ? null : new GitFileChange(path, worktree.Value, DiffScope.WorkingTree) };
    }).SelectMany(x => x).OfType<GitFileChange>().ToList();
    private static ChangeKind? Kind(char c) => c switch { ' ' => null, 'M' => ChangeKind.Modified, 'A' => ChangeKind.Added, 'D' => ChangeKind.Deleted, 'R' => ChangeKind.Renamed, '?' => ChangeKind.Untracked, 'U' => ChangeKind.Conflicted, _ => ChangeKind.Modified };
    private static IReadOnlyList<BranchInfo> ParseBranches(string text) => text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split('|')).Where(x => x.Length == 2).Select(x => new BranchInfo(x[1], x[0] == "*")).ToList();
    private static IReadOnlyList<CommitInfo> ParseHistory(string text) => text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split('\x1f')).Where(x => x.Length == 4).Select(x => new CommitInfo(x[0], x[1], x[2], DateTimeOffset.Parse(x[3], CultureInfo.InvariantCulture))).ToList();
    private static async Task<GitCommandResult> RunAsync(string? workingDirectory, IEnumerable<string> args, CancellationToken ct)
    {
        try
        {
            var start = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            if (!string.IsNullOrWhiteSpace(workingDirectory)) start.WorkingDirectory = workingDirectory;
            foreach (var arg in args) start.ArgumentList.Add(arg);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Git process could not be started.");
            var output = process.StandardOutput.ReadToEndAsync(ct); var error = process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);
            return new GitCommandResult(process.ExitCode, await output, await error);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        { return new GitCommandResult(-1, string.Empty, ex.Message); }
    }
}
