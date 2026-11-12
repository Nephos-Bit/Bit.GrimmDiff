using System.IO;
using System.Text.RegularExpressions;

namespace GrimmDiff.App;

public static partial class ReleaseNotesReader
{
    [GeneratedRegex("^# GrimmDiff (?<version>\\d+\\.\\d+\\.\\d+(?:[-+][0-9A-Za-z.-]+)?)\\s*$", RegexOptions.Multiline)]
    private static partial Regex TitlePattern();

    [GeneratedRegex("^## (?<language>Deutsch|English)\\s*$", RegexOptions.Multiline)]
    private static partial Regex SectionPattern();

    public static ReleaseNotes? Read(string path)
    {
        try
        {
            return Parse(File.ReadAllText(path));
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static ReleaseNotes? Parse(string content)
    {
        var title = TitlePattern().Match(content);
        if (!title.Success) return null;

        var sections = SectionPattern().Matches(content);
        if (sections.Count != 2) return null;
        var german = SectionBody(content, sections[0], sections[1].Index);
        var english = SectionBody(content, sections[1], content.Length);
        return sections[0].Groups["language"].Value == "Deutsch" && sections[1].Groups["language"].Value == "English" &&
               HasValidBulletCount(german) && HasValidBulletCount(english)
            ? new ReleaseNotes(title.Groups["version"].Value, german, english)
            : null;
    }

    private static string SectionBody(string content, Match section, int end) =>
        content[(section.Index + section.Length)..end].Trim();

    private static bool HasValidBulletCount(string section)
    {
        var count = section.Split('\n').Count(line => line.TrimStart().StartsWith("- ", StringComparison.Ordinal));
        return count is >= 3 and <= 6;
    }
}
