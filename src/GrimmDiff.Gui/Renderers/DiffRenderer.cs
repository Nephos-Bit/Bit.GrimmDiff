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
        var lines = new List<RenderLine>();
        var markers = new List<DiffTextMarkerModel>();
        var lineNumbers = new List<DiffLineNumbersModel>();

        while (position.HunkIndex < hunks.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hunk = hunks[position.HunkIndex];
            var content = hunk.Content.Split('\n');
            var lineIndex = position.ContentLineIndex;
            var line = lineIndex < 0 ? hunk.Header : content[lineIndex];
            var kind = Classify(line, lineIndex < 0);
            var additionalLength = line.Length + Environment.NewLine.Length;
            if (lines.Count > 0 && (lines.Count >= MaximumPageLines ||
                                    text.Length + additionalLength > MaximumPageCharacters)) break;

            var offset = text.Length;
            text.Append(line).AppendLine();
            lines.Add(new RenderLine(line, offset, kind));
            lineNumbers.Add(GetLineNumbers(hunk, content, lineIndex));
            if (kind is DiffHighlightKindModel.AddedLine or DiffHighlightKindModel.RemovedLine or DiffHighlightKindModel.Hunk)
                markers.Add(new DiffTextMarkerModel(offset, line.Length, kind.Value));

            if (lineIndex < 0) position = position with { ContentLineIndex = 0 };
            else if (lineIndex + 1 < content.Length) position = position with { ContentLineIndex = lineIndex + 1 };
            else position = new DiffPageCursorModel(position.HunkIndex + 1, -1);
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

    #endregion
}
