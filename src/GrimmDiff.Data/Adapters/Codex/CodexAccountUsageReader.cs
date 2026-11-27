using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using GrimmDiff.Logic.Abstracts.Models;

namespace GrimmDiff.Data.Adapters.Codex;

internal static class CodexAccountUsageReader
{
    private static readonly TimeSpan AccountUsageTimeout = TimeSpan.FromSeconds(20);
    public static async Task<CodexAccountUsageModel> GetAsync(string executable, IReadOnlyList<string> removedCredentialVariables, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo(executable)
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
        foreach (var variable in removedCredentialVariables) startInfo.Environment.Remove(variable);

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

}
