using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Data;

public sealed class GitCliRepositoryService
{
    public async Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default)
    {
        return (await RunAsync(null, ["--version"], cancellationToken)).IsSuccess;
    }

    public async Task<RepositorySnapshotModel> LoadAsync(string repositoryPath,
        CancellationToken cancellationToken = default)
    {
        var branch = await RunAsync(repositoryPath, ["branch", "--show-current"], cancellationToken);
        var porcelain = await RunAsync(repositoryPath, ["status", "--porcelain=v1", "-uall"], cancellationToken);
        var branches = await RunAsync(repositoryPath, ["branch", "--format=%(HEAD)|%(refname:short)"],
            cancellationToken);
        var history = await RunAsync(repositoryPath, ["log", "-30", "--format=%H%x1f%s%x1f%an%x1f%aI"],
            cancellationToken);
        var state = await GetStateAsync(repositoryPath, cancellationToken);
        return new RepositorySnapshotModel(repositoryPath, branch.StandardOutput.Trim(), state,
            ParseChanges(porcelain.StandardOutput), ParseBranches(branches.StandardOutput),
            ParseHistory(history.StandardOutput));
    }

    public async Task<string> GetDiffAsync(string repositoryPath, DiffScopeModel scope, string? path = null,
        CancellationToken cancellationToken = default)
    {
        var arguments = scope == DiffScopeModel.Staged
            ? new List<string>
                { "-c", "core.fsmonitor=false", "diff", "--cached", "--no-ext-diff", "--no-textconv", "--unified=3" }
            : ["-c", "core.fsmonitor=false", "diff", "--no-ext-diff", "--no-textconv", "--unified=3"];
        if (!string.IsNullOrWhiteSpace(path))
        {
            arguments.Add("--");
            arguments.Add(path);
        }

        var diff = (await RunAsync(repositoryPath, arguments, cancellationToken)).StandardOutput;
        return scope == DiffScopeModel.WorkingTree && string.IsNullOrWhiteSpace(path)
            ? diff + await GetUntrackedDiffAsync(repositoryPath, cancellationToken)
            : diff;
    }

    public async Task<string> GetCommitDiffAsync(string repositoryPath, string commitId, string? path = null,
        CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>
        {
            "-c", "core.fsmonitor=false", "show", "--format=", "--no-ext-diff", "--no-textconv", "--unified=3", commitId
        };
        if (!string.IsNullOrWhiteSpace(path))
        {
            arguments.Add("--");
            arguments.Add(path);
        }

        return (await RunAsync(repositoryPath, arguments, cancellationToken)).StandardOutput;
    }

    public Task<GitCommandResultModel> StageAsync(string repositoryPath, string path,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(repositoryPath, ["add", "--", path], cancellationToken);
    }

    public Task<GitCommandResultModel> UnstageAsync(string repositoryPath, string path,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(repositoryPath, ["restore", "--staged", "--", path], cancellationToken);
    }

    public Task<GitCommandResultModel> CommitAsync(string repositoryPath, string message,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(repositoryPath, ["commit", "-m", message], cancellationToken);
    }

    public Task<GitCommandResultModel> FetchAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        return RunAsync(repositoryPath, ["fetch"], cancellationToken);
    }

    public Task<GitCommandResultModel> PullAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        return RunAsync(repositoryPath, ["pull"], cancellationToken);
    }

    public Task<GitCommandResultModel> PushAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        return RunAsync(repositoryPath, ["push"], cancellationToken);
    }

    public Task<GitCommandResultModel> SwitchBranchAsync(string repositoryPath, string branch,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(repositoryPath, ["switch", branch], cancellationToken);
    }

    public Task<GitCommandResultModel> CreateBranchAsync(string repositoryPath, string branch,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(repositoryPath, ["switch", "-c", branch], cancellationToken);
    }

    public Task<GitCommandResultModel> CloneAsync(string source, string destination,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(null, ["clone", source, destination], cancellationToken);
    }

    #region Private

    private static async Task<RepositoryStateModel> GetStateAsync(string path, CancellationToken ct)
    {
        if (Directory.Exists(Path.Combine(path, ".git", "rebase-merge")) ||
            Directory.Exists(Path.Combine(path, ".git", "rebase-apply"))) return RepositoryStateModel.RebaseInProgress;
        if (File.Exists(Path.Combine(path, ".git", "MERGE_HEAD"))) return RepositoryStateModel.MergeInProgress;
        if (File.Exists(Path.Combine(path, ".git", "CHERRY_PICK_HEAD"))) return RepositoryStateModel.CherryPickInProgress;
        var status = await RunAsync(path, ["diff", "--name-only", "--diff-filter=U"], ct);
        return string.IsNullOrWhiteSpace(status.StandardOutput) ? RepositoryStateModel.Ready : RepositoryStateModel.Conflict;
    }

    private static async Task<string> GetUntrackedDiffAsync(string repositoryPath, CancellationToken cancellationToken)
    {
        var files = await RunAsync(repositoryPath, ["ls-files", "--others", "--exclude-standard", "-z"],
            cancellationToken);
        var paths = files.StandardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        var diffs = new List<string>();
        foreach (var file in paths)
        {
            // git diff returns exit code 1 when --no-index finds a difference; its output is the desired patch.
            var diff = await RunAsync(repositoryPath,
                ["diff", "--no-index", "--no-ext-diff", "--unified=3", "--", "/dev/null", file], cancellationToken);
            if (!string.IsNullOrWhiteSpace(diff.StandardOutput)) diffs.Add(diff.StandardOutput);
        }

        return diffs.Count == 0 ? string.Empty : "\n" + string.Join("\n", diffs);
    }

    private static IReadOnlyList<GitFileChangeModel> ParseChanges(string text)
    {
        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line =>
        {
            var index = Kind(line[0]);
            var worktree = Kind(line[1]);
            var path = line[3..];
            return new[]
            {
                index == null ? null : new GitFileChangeModel(path, index.Value, DiffScopeModel.Staged),
                worktree == null ? null : new GitFileChangeModel(path, worktree.Value, DiffScopeModel.WorkingTree)
            };
        }).SelectMany(x => x).OfType<GitFileChangeModel>().ToList();
    }

    private static ChangeKindModel? Kind(char c)
    {
        return c switch
        {
            ' ' => null, 'M' => ChangeKindModel.Modified, 'A' => ChangeKindModel.Added, 'D' => ChangeKindModel.Deleted,
            'R' => ChangeKindModel.Renamed, '?' => ChangeKindModel.Untracked, 'U' => ChangeKindModel.Conflicted,
            _ => ChangeKindModel.Modified
        };
    }

    private static IReadOnlyList<BranchInfoModel> ParseBranches(string text)
    {
        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split('|'))
            .Where(x => x.Length == 2).Select(x => new BranchInfoModel(x[1], x[0] == "*")).ToList();
    }

    private static IReadOnlyList<CommitInfoModel> ParseHistory(string text)
    {
        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Split('\x1f'))
            .Where(x => x.Length == 4).Select(x =>
                new CommitInfoModel(x[0], x[1], x[2], DateTimeOffset.Parse(x[3], CultureInfo.InvariantCulture))).ToList();
    }

    private static async Task<GitCommandResultModel> RunAsync(string? workingDirectory, IEnumerable<string> args,
        CancellationToken ct)
    {
        try
        {
            var command = args.FirstOrDefault() ?? string.Empty;
            var hardened = command switch
            {
                "fetch" or "pull" or "push" or "clone" => new[]
                        { "-c", "protocol.ext.allow=never", "-c", "core.fsmonitor=false", "-c", "core.hooksPath=" }
                    .Concat(args),
                _ => new[] { "-c", "core.fsmonitor=false", "-c", "core.hooksPath=", "-c", "diff.external=" }
                    .Concat(args)
            };
            var start = new ProcessStartInfo(ResolveGitExecutable())
            {
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
                CreateNoWindow = true
            };
            if (!string.IsNullOrWhiteSpace(workingDirectory)) start.WorkingDirectory = workingDirectory;
            foreach (var arg in hardened) start.ArgumentList.Add(arg);
            using var process = Process.Start(start) ??
                                throw new InvalidOperationException("Git process could not be started.");
            var output = process.StandardOutput.ReadToEndAsync(ct);
            var error = process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);
            return new GitCommandResultModel(process.ExitCode, await output, await error);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            return new GitCommandResultModel(-1, string.Empty, ex.Message);
        }
    }

    private static string ResolveGitExecutable()
    {
        if (!OperatingSystem.IsWindows()) return "git";
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "cmd", "git.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "cmd", "git.exe")
        };
        return candidates.FirstOrDefault(File.Exists) ??
               throw new FileNotFoundException("Git was not found in a trusted installation directory.");
    }

    #endregion
}