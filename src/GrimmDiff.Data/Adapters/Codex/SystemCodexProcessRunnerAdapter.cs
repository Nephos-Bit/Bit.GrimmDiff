using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using GrimmDiff.Data.Abstracts;

namespace GrimmDiff.Data;

public sealed class SystemCodexProcessRunnerAdapter : ICodexProcessRunnerPort
{
    public async Task<CodexProcessResultModel> RunAsync(CodexProcessRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo(request.Executable)
        {
            WorkingDirectory = request.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = request.StandardInput is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var argument in request.Arguments) startInfo.ArgumentList.Add(argument);
        foreach (var variable in request.EnvironmentVariablesToRemove) startInfo.Environment.Remove(variable);

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var errorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);
        if (request.StandardInput is not null)
            try
            {
                await process.StandardInput.WriteAsync(request.StandardInput.AsMemory(), cancellationToken);
                process.StandardInput.Close();
            }
            catch
            {
                TryKill(process);
                throw;
            }

        using var timeout = new CancellationTokenSource(request.Timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            await process.WaitForExitAsync(CancellationToken.None);
            if (cancellationToken.IsCancellationRequested) throw;
            return new CodexProcessResultModel(process.ExitCode, await outputTask, await errorTask, true);
        }

        return new CodexProcessResultModel(process.ExitCode, await outputTask, await errorTask);
    }

    #region Private

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited) process.Kill(true);
        }
        catch (InvalidOperationException)
        {
        }
        catch (Win32Exception)
        {
        }
    }

    #endregion
}
