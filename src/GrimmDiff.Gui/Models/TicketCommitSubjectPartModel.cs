namespace GrimmDiff.Gui.Models;

public sealed record TicketCommitSubjectPartModel(string Text, string Url) : CommitSubjectPartModel(Text);
