using System.Diagnostics;
using System.Text;
using GrimmDiff.Data;
using GrimmDiff.Logic.Abstracts;

var repositoryPath = Path.Combine(AppContext.BaseDirectory, "GrimmDiff-utf8-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(repositoryPath);
try
{
    RunGit("init", "-q");
    RunGit("config", "user.name", "GrimmDiff Test");
    RunGit("config", "user.email", "test@example.com");

    const string fileName = "Änderung äöÖÜü.txt";
    const string umlauts = "Ä ä ö Ö Ü ü";
    var filePath = Path.Combine(repositoryPath, fileName);
    await File.WriteAllTextAsync(filePath, "vorher\n", new UTF8Encoding(false));
    RunGit("add", "--", fileName);
    RunGit("commit", "-q", "-m", "Initial");

    await File.WriteAllTextAsync(filePath, umlauts + "\n", new UTF8Encoding(false));
    var service = new GitCliRepositoryService();
    AssertDiff(await service.GetDiffAsync(repositoryPath, DiffScopeModel.WorkingTree), fileName, umlauts);
    RunGit("add", "--", fileName);
    AssertDiff(await service.GetDiffAsync(repositoryPath, DiffScopeModel.Staged), fileName, umlauts);
    RunGit("commit", "-q", "-m", "Umlaute");
    AssertDiff(await service.GetCommitDiffAsync(repositoryPath, "HEAD"), fileName, umlauts);

    const string untrackedName = "Neu Ü.txt";
    await File.WriteAllTextAsync(Path.Combine(repositoryPath, untrackedName), umlauts + "\n", new UTF8Encoding(false));
    AssertDiff(await service.GetDiffAsync(repositoryPath, DiffScopeModel.WorkingTree), untrackedName, umlauts);

    Console.WriteLine("PASS UTF-8 diff content and file names in working tree, index, history, and untracked files");
}
finally
{
    foreach (var file in Directory.EnumerateFiles(repositoryPath, "*", SearchOption.AllDirectories))
        File.SetAttributes(file, FileAttributes.Normal);
    Directory.Delete(repositoryPath, true);
}

void RunGit(params string[] arguments)
{
    var start = new ProcessStartInfo("git")
    {
        WorkingDirectory = repositoryPath,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        StandardErrorEncoding = Encoding.UTF8
    };
    foreach (var argument in arguments) start.ArgumentList.Add(argument);
    using var process = Process.Start(start) ?? throw new Exception("Git could not be started.");
    var error = process.StandardError.ReadToEnd();
    process.WaitForExit();
    if (process.ExitCode != 0) throw new Exception($"Git {arguments[0]} failed: {error}");
}

static void AssertDiff(string diff, string fileName, string text)
{
    if (!UnifiedDiffParser.Parse(diff, DiffScopeModel.WorkingTree).Any(hunk =>
            hunk.FilePath == fileName && hunk.Content.Contains("+" + text, StringComparison.Ordinal)))
        throw new Exception($"Diff did not preserve UTF-8 text and file name: {diff}");
}
