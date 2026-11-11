using System.Text;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using GrimmDiff.Domain;

namespace GrimmDiff.App;

public enum DiffHighlightKind { AddedLine, RemovedLine, Hunk, AddedWord, RemovedWord }

public sealed record DiffTextMarker(int Offset, int Length, DiffHighlightKind Kind);

public sealed record DiffPageCursor(int HunkIndex, int ContentLineIndex);

public sealed record DiffRenderPage(string Text, IReadOnlyList<DiffTextMarker> Markers, DiffPageCursor? NextPage);

public sealed class DiffRenderDocument
{
    public DiffRenderDocument(string text, IReadOnlyList<DiffTextMarker> markers, DiffPageCursor? nextPage) => (Text, Markers, NextPage) = (text, markers, nextPage);
    public string Text { get; }
    public IReadOnlyList<DiffTextMarker> Markers { get; }
    public DiffPageCursor? NextPage { get; }
    public static DiffRenderDocument FromPage(DiffRenderPage page) => new(page.Text, page.Markers, page.NextPage);

    public DiffRenderDocument Append(DiffRenderPage page)
    {
        var offset = Text.Length;
        var markers = new List<DiffTextMarker>(Markers.Count + page.Markers.Count);
        markers.AddRange(Markers);
        markers.AddRange(page.Markers.Select(marker => marker with { Offset = marker.Offset + offset }));
        return new(Text + page.Text, markers, page.NextPage);
    }
}

public static class DiffRenderBuilder
{
    public const int MaximumPageLines = 2_000;
    public const int MaximumPageCharacters = 1_048_576;

    public static DiffRenderPage CreatePage(IReadOnlyList<DiffHunk> hunks, DiffPageCursor? cursor = null, CancellationToken cancellationToken = default)
    {
        if (hunks.Count == 0) return new(string.Empty, [], null);
        var position = cursor ?? new DiffPageCursor(0, -1);
        var text = new StringBuilder();
        var lines = new List<RenderLine>();
        var markers = new List<DiffTextMarker>();

        while (position.HunkIndex < hunks.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hunk = hunks[position.HunkIndex];
            var content = hunk.Content.Split('\n');
            var lineIndex = position.ContentLineIndex;
            var line = lineIndex < 0 ? hunk.Header : content[lineIndex];
            var kind = Classify(line, lineIndex < 0);
            var additionalLength = line.Length + Environment.NewLine.Length;
            if (lines.Count > 0 && (lines.Count >= MaximumPageLines || text.Length + additionalLength > MaximumPageCharacters)) break;

            var offset = text.Length;
            text.Append(line).AppendLine();
            lines.Add(new RenderLine(line, offset, kind));
            if (kind is DiffHighlightKind.AddedLine or DiffHighlightKind.RemovedLine or DiffHighlightKind.Hunk)
                markers.Add(new DiffTextMarker(offset, line.Length, kind.Value));

            if (lineIndex < 0) position = position with { ContentLineIndex = 0 };
            else if (lineIndex + 1 < content.Length) position = position with { ContentLineIndex = lineIndex + 1 };
            else position = new DiffPageCursor(position.HunkIndex + 1, -1);
        }

        AddWordMarkers(lines, markers, cancellationToken);
        var nextPage = position.HunkIndex < hunks.Count ? position : null;
        return new(text.ToString(), markers, nextPage);
    }

    #region Private

    private static DiffHighlightKind? Classify(string line, bool isHeader)
    {
        if (isHeader || line.StartsWith("@@", StringComparison.Ordinal)) return DiffHighlightKind.Hunk;
        if (line.StartsWith('+') && !line.StartsWith("+++", StringComparison.Ordinal)) return DiffHighlightKind.AddedLine;
        if (line.StartsWith('-') && !line.StartsWith("---", StringComparison.Ordinal)) return DiffHighlightKind.RemovedLine;
        return null;
    }

    private static void AddWordMarkers(IReadOnlyList<RenderLine> lines, ICollection<DiffTextMarker> markers, CancellationToken cancellationToken)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (lines[index].Kind != DiffHighlightKind.RemovedLine) continue;
            var removedStart = index;
            while (index < lines.Count && lines[index].Kind == DiffHighlightKind.RemovedLine) index++;
            var addedStart = index;
            while (index < lines.Count && lines[index].Kind == DiffHighlightKind.AddedLine) index++;
            if (addedStart == index) continue;

            var removed = lines.Skip(removedStart).Take(addedStart - removedStart).ToList();
            var added = lines.Skip(addedStart).Take(index - addedStart).ToList();
            AddWordMarkers(removed, added, markers);
            index--;
        }
    }

    private static void AddWordMarkers(IReadOnlyList<RenderLine> removed, IReadOnlyList<RenderLine> added, ICollection<DiffTextMarker> markers)
    {
        var oldText = string.Join('\n', removed.Select(line => line.Text[1..]));
        var newText = string.Join('\n', added.Select(line => line.Text[1..]));
        var model = SideBySideDiffBuilder.Instance.BuildDiffModel(oldText, newText);
        AddWordMarkers(model.OldText.Lines, removed, DiffHighlightKind.RemovedWord, markers);
        AddWordMarkers(model.NewText.Lines, added, DiffHighlightKind.AddedWord, markers);
    }

    private static void AddWordMarkers(IReadOnlyList<DiffPiece> pieces, IReadOnlyList<RenderLine> lines, DiffHighlightKind kind, ICollection<DiffTextMarker> markers)
    {
        foreach (var piece in pieces)
        {
            if (piece.Type != ChangeType.Modified || piece.Position is not int position || position < 1 || position > lines.Count) continue;
            var line = lines[position - 1];
            var offset = line.Offset + 1;
            foreach (var subPiece in piece.SubPieces)
            {
                var length = subPiece.Text?.Length ?? 0;
                if (length > 0 && ((kind == DiffHighlightKind.RemovedWord && subPiece.Type == ChangeType.Deleted) || (kind == DiffHighlightKind.AddedWord && subPiece.Type == ChangeType.Inserted)))
                    markers.Add(new DiffTextMarker(offset, length, kind));
                offset += length;
            }
        }
    }

    private sealed record RenderLine(string Text, int Offset, DiffHighlightKind? Kind);

    #endregion
}
