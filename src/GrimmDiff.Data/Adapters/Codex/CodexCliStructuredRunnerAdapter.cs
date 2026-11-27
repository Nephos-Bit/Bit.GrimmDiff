using System.Text;
using GrimmDiff.Data.Abstracts.Ports;
using GrimmDiff.Data.Exceptions;
using GrimmDiff.Logic.Abstracts.Ports;

namespace GrimmDiff.Data.Adapters.Codex;

public sealed class CodexCliStructuredRunnerAdapter(ICodexProcessRunnerPort? processRunner = null) : ICodexStructuredRunnerPort
{
    private readonly ICodexProcessRunnerPort _processRunner = processRunner ?? new SystemCodexProcessRunnerAdapter();
    private readonly bool _resolveSystemExecutable = processRunner is null;

    public async Task<string> RunStructuredAsync(string prompt, string schema, TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var executable = CodexCliProcessConfiguration.ResolveExecutable(_resolveSystemExecutable);
        var tempDirectory = Path.Combine(Path.GetTempPath(), "GrimmDiff", Guid.NewGuid().ToString("N"));
        var schemaPath = Path.Combine(tempDirectory, "analysis.schema.json");
        var outputPath = Path.Combine(tempDirectory, "analysis.result.json");
        Directory.CreateDirectory(tempDirectory);
        await File.WriteAllTextAsync(schemaPath, schema, new UTF8Encoding(false), cancellationToken);
        try
        {
            string[] arguments =
            [
                "exec", "-m", "gpt-6-luna", "--ephemeral", "--ignore-user-config",
                "--sandbox", "read-only", "-c", "approval_policy=\"never\"",
                "-c", "web_search=\"disabled\"", "--skip-git-repo-check", "--output-schema", schemaPath,
                "--output-last-message", outputPath, "--color", "never", "-"
            ];
            var result = await _processRunner.RunAsync(
                CodexCliProcessConfiguration.Request(executable, arguments, prompt, timeout, tempDirectory),
                cancellationToken);
            if (result.TimedOut) throw new TimeoutException("GPT-6 Luna analysis timed out.");
            if (result.ExitCode != 0)
                throw new CodexCliInvocationException("GPT-6 Luna analysis failed.", Limit(result.StandardError));
            var output = File.Exists(outputPath)
                ? await File.ReadAllTextAsync(outputPath, cancellationToken)
                : result.StandardOutput;
            if (string.IsNullOrWhiteSpace(output))
                throw new CodexCliInvocationException("GPT-6 Luna returned no result.", Limit(result.StandardError));
            return output.Trim();
        }
        finally
        {
            try
            {
                Directory.Delete(tempDirectory, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static string Limit(string value) => value.Length <= 2000 ? value : value[^2000..];
}
