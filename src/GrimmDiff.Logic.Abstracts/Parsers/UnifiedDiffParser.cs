using GrimmDiff.Logic.Abstracts;
using System.Text.RegularExpressions;

namespace GrimmDiff.Logic.Abstracts;

public static partial class UnifiedDiffParser
{
    public static IReadOnlyList<DiffHunkModel> Parse(string diff, DiffScopeModel scope)
    {
        var hunks = new List<DiffHunkModel>();
        var currentPath = string.Empty;
        var lines = diff.Replace("\r\n", "\n").Split('\n');
        DiffHunkBuilder? current = null;
        var fileLines = new List<string>();
        var hasHunk = false;
        foreach (var line in lines)
        {
            var file = FileHeader().Match(line);
            if (file.Success)
            {
                AddFile();
                currentPath = file.Groups[2].Value;
                hasHunk = false;
                continue;
            }

            var header = HunkHeader().Match(line);
            if (header.Success)
            {
                AddCurrent();
                current = new DiffHunkBuilder(currentPath, line, int.Parse(header.Groups[1].Value),
                    int.Parse(header.Groups[2].Value));
                hasHunk = true;
                continue;
            }

            if (current is null) fileLines.Add(line);
            else current.Lines.Add(line);
        }

        AddFile();
        return hunks;

        void AddCurrent()
        {
            if (current is not null)
            {
                hunks.Add(new DiffHunkModel(current.Path, scope, current.Header, string.Join('\n', current.Lines),
                    current.OldStart, current.NewStart));
                current = null;
            }
        }

        void AddFile()
        {
            AddCurrent();
            if (!string.IsNullOrEmpty(currentPath) && !hasHunk)
                hunks.Add(new DiffHunkModel(currentPath, scope, "File metadata", string.Join('\n', fileLines), 0, 0));
            fileLines.Clear();
        }
    }

    #region Private

    [GeneratedRegex("^diff --git a/(.+?) b/(.+)$")]
    private static partial Regex FileHeader();

    [GeneratedRegex("^@@ -(\\d+)(?:,\\d+)? \\+(\\d+)(?:,\\d+)? @@(.*)$")]
    private static partial Regex HunkHeader();

    private sealed class DiffHunkBuilder(string path, string header, int oldStart, int newStart)
    {
        public string Path { get; } = path;
        public string Header { get; } = header;
        public int OldStart { get; } = oldStart;
        public int NewStart { get; } = newStart;
        public List<string> Lines { get; } = [];
    }

    #endregion
}