using System.Text;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using GrimmDiff.Logic.Abstracts;

namespace GrimmDiff.Gui;

public static class DiffRenderer
{
    public const int MaximumPageLines = 2_000;
    public const int MaximumPageCharacters = 1_048_576;

    public static DiffRenderPageModel CreatePage(IReadOnlyList<DiffHunkModel> hunks, DiffPageCursorModel? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (hunks.Count == 0) return new DiffRenderPageModel(string.Empty, [], [], null);
        var position = cursor ?? new DiffPageCursorModel(0, -1);
        var text = new StringBuilder();
        var pendingLines = new List<PendingLine>();
        var pendingCharacters = 0;

        while (position.HunkIndex < hunks.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hunk = hunks[position.HunkIndex];
            var content = hunk.Content.Split('\n');
            var lineIndex = position.ContentLineIndex;
            var sourceLine = lineIndex < 0 ? hunk.Header : content[lineIndex];
            var isHeader = lineIndex < 0;
            var additionalLength = sourceLine.Length + Environment.NewLine.Length;
            if (pendingLines.Count > 0 && (pendingLines.Count >= MaximumPageLines ||
                                    pendingCharacters + additionalLength > MaximumPageCharacters)) break;

            pendingLines.Add(new PendingLine(sourceLine, isHeader, GetLineNumbers(hunk, content, lineIndex)));
            pendingCharacters += additionalLength;

            if (lineIndex < 0) position = position with { ContentLineIndex = 0 };
            else if (lineIndex + 1 < content.Length) position = position with { ContentLineIndex = lineIndex + 1 };
            else position = new DiffPageCursorModel(position.HunkIndex + 1, -1);
        }

        var commonIndentation = GetCommonIndentation(pendingLines);
        var lines = new List<RenderLine>(pendingLines.Count);
        var markers = new List<DiffTextMarkerModel>();
        var lineNumbers = new List<DiffLineNumbersModel>(pendingLines.Count);
        foreach (var pendingLine in pendingLines)
        {
            var line = AlignLeft(pendingLine.Text, pendingLine.IsHeader, commonIndentation);
            var kind = Classify(line, pendingLine.IsHeader);
            var offset = text.Length;
            text.Append(line).AppendLine();
            lines.Add(new RenderLine(line, offset, kind));
            lineNumbers.Add(pendingLine.LineNumbers);
            if (kind is DiffHighlightKindModel.AddedLine or DiffHighlightKindModel.RemovedLine or DiffHighlightKindModel.Hunk)
                markers.Add(new DiffTextMarkerModel(offset, line.Length, kind.Value));
        }

        AddWordMarkers(lines, markers, cancellationToken);
        var nextPage = position.HunkIndex < hunks.Count ? position : null;
        return new DiffRenderPageModel(text.ToString(), markers, lineNumbers, nextPage);
    }

    #region Private

    private static DiffHighlightKindModel? Classify(string line, bool isHeader)
    {
        if (isHeader || line.StartsWith("@@", StringComparison.Ordinal)) return DiffHighlightKindModel.Hunk;
        if (line.StartsWith('+') && !line.StartsWith("+++", StringComparison.Ordinal))
            return DiffHighlightKindModel.AddedLine;
        if (line.StartsWith('-') && !line.StartsWith("---", StringComparison.Ordinal))
            return DiffHighlightKindModel.RemovedLine;
        return null;
    }

    private static int GetCommonIndentation(IReadOnlyList<PendingLine> lines)
    {
        var indentation = int.MaxValue;
        foreach (var line in lines)
        {
            if (line.IsHeader || line.Text.StartsWith('\\')) continue;
            var start = HasDiffPrefix(line.Text) ? 1 : 0;
            var length = line.Text.Length - start;
            if (length == 0 || string.IsNullOrWhiteSpace(line.Text[start..])) continue;
            var count = line.Text[start..].TakeWhile(char.IsWhiteSpace).Count();
            indentation = Math.Min(indentation, count);
        }

        return indentation == int.MaxValue ? 0 : indentation;
    }

    private static string AlignLeft(string line, bool isHeader, int commonIndentation)
    {
        if (isHeader || commonIndentation == 0) return line;
        var start = HasDiffPrefix(line) ? 1 : 0;
        var removable = line[start..].TakeWhile(char.IsWhiteSpace).Take(commonIndentation).Count();
        return line.Remove(start, removable);
    }

    private static bool HasDiffPrefix(string line)
    {
        return line.StartsWith('+') || line.StartsWith('-') || line.StartsWith(' ');
    }

    private static DiffLineNumbersModel GetLineNumbers(DiffHunkModel hunk, IReadOnlyList<string> content, int lineIndex)
    {
        if (lineIndex < 0) return new DiffLineNumbersModel(null, null);

        var oldLine = hunk.OldStart;
        var newLine = hunk.NewStart;
        for (var index = 0; index < lineIndex; index++) AdvanceLineNumbers(content[index], ref oldLine, ref newLine);

        return content[lineIndex] switch
        {
            var line when line.StartsWith('+') && !line.StartsWith("+++", StringComparison.Ordinal) =>
                new DiffLineNumbersModel(null, newLine),
            var line when line.StartsWith('-') && !line.StartsWith("---", StringComparison.Ordinal) =>
                new DiffLineNumbersModel(oldLine, null),
            var line when line.StartsWith('\\') => new DiffLineNumbersModel(null, null),
            _ => new DiffLineNumbersModel(oldLine, newLine)
        };
    }

    private static void AdvanceLineNumbers(string line, ref int oldLine, ref int newLine)
    {
        if (line.StartsWith('+') && !line.StartsWith("+++", StringComparison.Ordinal)) newLine++;
        else if (line.StartsWith('-') && !line.StartsWith("---", StringComparison.Ordinal)) oldLine++;
        else if (!line.StartsWith('\\'))
        {
            oldLine++;
            newLine++;
        }
    }

    private static void AddWordMarkers(IReadOnlyList<RenderLine> lines, ICollection<DiffTextMarkerModel> markers,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (lines[index].Kind != DiffHighlightKindModel.RemovedLine) continue;
            var removedStart = index;
            while (index < lines.Count && lines[index].Kind == DiffHighlightKindModel.RemovedLine) index++;
            var addedStart = index;
            while (index < lines.Count && lines[index].Kind == DiffHighlightKindModel.AddedLine) index++;
            if (addedStart == index) continue;

            var removed = lines.Skip(removedStart).Take(addedStart - removedStart).ToList();
            var added = lines.Skip(addedStart).Take(index - addedStart).ToList();
            AddWordMarkers(removed, added, markers);
            index--;
        }
    }

    private static void AddWordMarkers(IReadOnlyList<RenderLine> removed, IReadOnlyList<RenderLine> added,
        ICollection<DiffTextMarkerModel> markers)
    {
        var oldText = string.Join('\n', removed.Select(line => line.Text[1..]));
        var newText = string.Join('\n', added.Select(line => line.Text[1..]));
        var model = SideBySideDiffBuilder.Instance.BuildDiffModel(oldText, newText);
        AddWordMarkers(model.OldText.Lines, removed, DiffHighlightKindModel.RemovedWord, markers);
        AddWordMarkers(model.NewText.Lines, added, DiffHighlightKindModel.AddedWord, markers);
    }

    private static void AddWordMarkers(IReadOnlyList<DiffPiece> pieces, IReadOnlyList<RenderLine> lines,
        DiffHighlightKindModel kind, ICollection<DiffTextMarkerModel> markers)
    {
        foreach (var piece in pieces)
        {
            if (piece.Type != ChangeType.Modified || piece.Position is not int position || position < 1 ||
                position > lines.Count) continue;
            var line = lines[position - 1];
            var offset = line.Offset + 1;
            foreach (var subPiece in piece.SubPieces)
            {
                var length = subPiece.Text?.Length ?? 0;
                if (length > 0 && ((kind == DiffHighlightKindModel.RemovedWord && subPiece.Type == ChangeType.Deleted) ||
                                   (kind == DiffHighlightKindModel.AddedWord && subPiece.Type == ChangeType.Inserted)))
                    markers.Add(new DiffTextMarkerModel(offset, length, kind));
                offset += length;
            }
        }
    }

    private sealed record RenderLine(string Text, int Offset, DiffHighlightKindModel? Kind);
    private sealed record PendingLine(string Text, bool IsHeader, DiffLineNumbersModel LineNumbers);

    #endregion
}
