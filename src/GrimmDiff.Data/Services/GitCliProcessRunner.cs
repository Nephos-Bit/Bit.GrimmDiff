using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using GrimmDiff.Logic.Abstracts.Models;

namespace GrimmDiff.Data.Services;

internal static class GitCliProcessRunner
{
    public static async Task<GitCommandResultModel> RunAsync(string? workingDirectory, IEnumerable<string> args,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = args.FirstOrDefault() ?? string.Empty;
            var hardened = command switch
            {
                "pull" or "clone" => new[]
                        { "-c", "protocol.ext.allow=never", "-c", "core.fsmonitor=false", "-c", "core.hooksPath=" }
                    .Concat(args),
                _ => new[] { "-c", "core.fsmonitor=false", "-c", "core.hooksPath=", "-c", "diff.external=" }
                    .Concat(args)
            };
            var start = new ProcessStartInfo(ResolveGitExecutable())
            {
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
                CreateNoWindow = true, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
            };
            if (!string.IsNullOrWhiteSpace(workingDirectory)) start.WorkingDirectory = workingDirectory;
            foreach (var arg in hardened) start.ArgumentList.Add(arg);
            using var process = Process.Start(start) ??
                                throw new InvalidOperationException("Git process could not be started.");
            var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
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
}
