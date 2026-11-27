using GrimmDiff.Data.Services;
using GrimmDiff.Logic.Abstracts.Models;
using GrimmDiff.Logic.Abstracts.Ports;


namespace GrimmDiff.Data.Adapters.Git;

public sealed class GitCliRepositoryReaderAdapter : IRepositoryReaderPort
{
    public async Task<RepositorySnapshotModel> LoadAsync(string repositoryPath, CancellationToken cancellationToken = default)
    {
        var branch = await GitCliProcessRunner.RunAsync(repositoryPath, ["branch", "--show-current"], cancellationToken);
        var porcelain = await GitCliProcessRunner.RunAsync(repositoryPath, ["status", "--porcelain=v1", "-uall"], cancellationToken);
        var branches = await GitCliProcessRunner.RunAsync(repositoryPath, ["branch", "--format=%(HEAD)|%(refname:short)"],
            cancellationToken);
        var history = await GitCliProcessRunner.RunAsync(repositoryPath, ["log", "-30", "--format=%H%x1f%s%x1f%an%x1f%aI"],
            cancellationToken);
        var state = await GetStateAsync(repositoryPath, cancellationToken);
        return new RepositorySnapshotModel(repositoryPath, branch.StandardOutput.Trim(), state,
            GitCliSnapshotParser.ParseChanges(porcelain.StandardOutput),
            GitCliSnapshotParser.ParseBranches(branches.StandardOutput),
            GitCliSnapshotParser.ParseHistory(history.StandardOutput));
    }

    private static async Task<RepositoryStateModel> GetStateAsync(string path, CancellationToken cancellationToken)
    {
        if (Directory.Exists(Path.Combine(path, ".git", "rebase-merge")) ||
            Directory.Exists(Path.Combine(path, ".git", "rebase-apply"))) return RepositoryStateModel.RebaseInProgress;
        if (File.Exists(Path.Combine(path, ".git", "MERGE_HEAD"))) return RepositoryStateModel.MergeInProgress;
        if (File.Exists(Path.Combine(path, ".git", "CHERRY_PICK_HEAD"))) return RepositoryStateModel.CherryPickInProgress;
        var status = await GitCliProcessRunner.RunAsync(path, ["diff", "--name-only", "--diff-filter=U"], cancellationToken);
        return string.IsNullOrWhiteSpace(status.StandardOutput) ? RepositoryStateModel.Ready : RepositoryStateModel.Conflict;
    }
}
