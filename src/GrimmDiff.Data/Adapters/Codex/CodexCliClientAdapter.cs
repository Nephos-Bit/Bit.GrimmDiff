using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using GrimmDiff.Data.Abstracts;
using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Data;

public sealed class CodexCliClientAdapter(ICodexProcessRunnerPort? processRunner = null) : ICodexSessionClientPort, ICodexStructuredRunnerPort
{
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan AccountUsageTimeout = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan LoginTimeout = TimeSpan.FromMinutes(5);

    private static readonly string[] RemovedCredentialVariables =
        ["OPENAI_API_KEY", "CODEX_API_KEY", "CODEX_ACCESS_TOKEN"];

    private readonly ICodexProcessRunnerPort _processRunner = processRunner ?? new SystemCodexProcessRunnerAdapter();
    private readonly bool _resolveSystemExecutable = processRunner is null;

    public async Task<CodexCliStatusModel> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var executable = ResolveExecutable();
        try
        {
            var result = await _processRunner.RunAsync(Request(executable, ["login", "status"], null, StatusTimeout),
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
                await _processRunner.RunAsync(Request(executable, ["login"], null, LoginTimeout), cancellationToken);
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

    public async Task<CodexAccountUsageModel> GetAccountUsageAsync(CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo(ResolveExecutable())
        {
            WorkingDirectory = Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        startInfo.ArgumentList.Add("app-server");
        startInfo.ArgumentList.Add("--stdio");
        foreach (var variable in RemovedCredentialVariables) startInfo.Environment.Remove(variable);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
            _ = process.StandardError.ReadToEndAsync(CancellationToken.None);
            using var timeout = new CancellationTokenSource(AccountUsageTimeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

            await WriteRpcAsync(process,
                """{"method":"initialize","id":1,"params":{"clientInfo":{"name":"grimmdiff","title":"GrimmDiff","version":"1.0.0"}}}""",
                linked.Token);
            await ReadRpcResultAsync(process, 1, linked.Token);
            await WriteRpcAsync(process, """{"method":"initialized","params":{}}""", linked.Token);
            await WriteRpcAsync(process, """{"method":"account/read","id":2,"params":{"refreshToken":false}}""",
                linked.Token);
            await WriteRpcAsync(process,
                """{"method":"account/rateLimits/read","id":3,"params":{"excludeResetCreditDetails":true}}""",
                linked.Token);

            string? email = null;
            int? weeklyRemaining = null;
            var pending = new HashSet<int> { 2, 3 };
            while (pending.Count > 0)
            {
                using var response = await ReadRpcMessageAsync(process, linked.Token);
                if (!response.RootElement.TryGetProperty("id", out var idElement) ||
                    !idElement.TryGetInt32(out var id) || !pending.Remove(id)) continue;
                if (!response.RootElement.TryGetProperty("result", out var result)) continue;
                if (id == 2) email = ReadEmail(result);
                if (id == 3) weeklyRemaining = ReadWeeklyRemaining(result);
            }

            return new CodexAccountUsageModel(email, weeklyRemaining);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new CodexAccountUsageModel(null, null);
        }
        catch (Win32Exception)
        {
            return new CodexAccountUsageModel(null, null);
        }
        catch (FileNotFoundException)
        {
            return new CodexAccountUsageModel(null, null);
        }
        catch (InvalidDataException)
        {
            return new CodexAccountUsageModel(null, null);
        }
        catch (JsonException)
        {
            return new CodexAccountUsageModel(null, null);
        }
        finally
        {
            StopProcess(process);
        }
    }

    public async Task<string> RunStructuredAsync(string prompt, string schema, TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var executable = ResolveExecutable();
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
            var result = await _processRunner.RunAsync(Request(executable, arguments, prompt, timeout, tempDirectory),
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

    #region Private

    private static CodexProcessRequestModel Request(string executable, IReadOnlyList<string> arguments, string? input,
        TimeSpan timeout, string? workingDirectory = null)
    {
        return new CodexProcessRequestModel(executable, arguments, workingDirectory ?? Environment.CurrentDirectory, input,
            timeout,
            RemovedCredentialVariables);
    }

    private static async Task WriteRpcAsync(Process process, string message, CancellationToken cancellationToken)
    {
        await process.StandardInput.WriteLineAsync(message.AsMemory(), cancellationToken);
        await process.StandardInput.FlushAsync(cancellationToken);
    }

    private static async Task ReadRpcResultAsync(Process process, int expectedId, CancellationToken cancellationToken)
    {
        while (true)
        {
            using var response = await ReadRpcMessageAsync(process, cancellationToken);
            if (response.RootElement.TryGetProperty("id", out var id) && id.TryGetInt32(out var value) &&
                value == expectedId) return;
        }
    }

    private static async Task<JsonDocument> ReadRpcMessageAsync(Process process, CancellationToken cancellationToken)
    {
        var line = await process.StandardOutput.ReadLineAsync(cancellationToken);
        if (line is null) throw new InvalidDataException("Codex app-server closed before returning account usage.");
        return JsonDocument.Parse(line);
    }

    private static string? ReadEmail(JsonElement result)
    {
        if (!result.TryGetProperty("account", out var account) || account.ValueKind != JsonValueKind.Object)
            return null;
        if (!account.TryGetProperty("type", out var type) ||
            !string.Equals(type.GetString(), "chatgpt", StringComparison.Ordinal)) return null;
        return account.TryGetProperty("email", out var email) && email.ValueKind == JsonValueKind.String
            ? email.GetString()
            : null;
    }

    private static int? ReadWeeklyRemaining(JsonElement result)
    {
        var weeklyWindows = new List<(long Duration, int Used)>();
        if (result.TryGetProperty("rateLimits", out var rateLimits)) AddWindows(rateLimits, weeklyWindows);
        if (result.TryGetProperty("rateLimitsByLimitId", out var buckets) && buckets.ValueKind == JsonValueKind.Object)
            foreach (var bucket in buckets.EnumerateObject())
                AddWindows(bucket.Value, weeklyWindows);

        var weekly = weeklyWindows
            .Where(window => window.Duration is >= 6 * 24 * 60 and <= 8 * 24 * 60)
            .OrderBy(window => Math.Abs(7 * 24 * 60 - window.Duration))
            .FirstOrDefault();
        return weekly.Duration == 0 ? null : Math.Clamp(100 - weekly.Used, 0, 100);
    }

    private static void AddWindows(JsonElement snapshot, ICollection<(long Duration, int Used)> windows)
    {
        AddWindow(snapshot, "primary", windows);
        AddWindow(snapshot, "secondary", windows);
    }

    private static void AddWindow(JsonElement snapshot, string propertyName,
        ICollection<(long Duration, int Used)> windows)
    {
        if (!snapshot.TryGetProperty(propertyName, out var window) || window.ValueKind != JsonValueKind.Object) return;
        if (!window.TryGetProperty("windowDurationMins", out var duration) ||
            !duration.TryGetInt64(out var minutes)) return;
        if (!window.TryGetProperty("usedPercent", out var used) || !used.TryGetInt32(out var usedPercent)) return;
        windows.Add((minutes, usedPercent));
    }

    private static void StopProcess(Process process)
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

    private string ResolveExecutable()
    {
        return _resolveSystemExecutable ? ResolveSystemExecutable() : "codex";
    }

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

    private static string Limit(string value)
    {
        return value.Length <= 2000 ? value : value[^2000..];
    }

    #endregion
}
