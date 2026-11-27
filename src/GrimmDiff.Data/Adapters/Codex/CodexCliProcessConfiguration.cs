using GrimmDiff.Data.Abstracts.Models;

namespace GrimmDiff.Data.Adapters.Codex;

internal static class CodexCliProcessConfiguration
{
    public static IReadOnlyList<string> RemovedCredentialVariables { get; } =
        ["OPENAI_API_KEY", "CODEX_API_KEY", "CODEX_ACCESS_TOKEN"];

    public static CodexProcessRequestModel Request(string executable, IReadOnlyList<string> arguments, string? input,
        TimeSpan timeout, string? workingDirectory = null) =>
        new(executable, arguments, workingDirectory ?? Environment.CurrentDirectory, input, timeout,
            RemovedCredentialVariables);

    public static string ResolveExecutable(bool resolveSystemExecutable) =>
        resolveSystemExecutable ? ResolveSystemExecutable() : "codex";

    private static string ResolveSystemExecutable()
    {
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var root = Path.Combine(localAppData, "OpenAI", "Codex", "bin");
            try
            {
                var candidate = Directory.Exists(root)
                    ? Directory.EnumerateFiles(root, "codex.exe", SearchOption.AllDirectories)
                        .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
                    : null;
                if (candidate is not null) return candidate;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
        throw new FileNotFoundException("A Codex CLI installed by OpenAI was not found.");
    }
}
