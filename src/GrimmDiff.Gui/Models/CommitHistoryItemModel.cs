using GrimmDiff.Logic.Abstracts.Models;

namespace GrimmDiff.Gui.Models;

public sealed class CommitHistoryItemModel
{
    public CommitHistoryItemModel(CommitInfoModel commit, TicketLinkSettingsModel? ticketLinks = null)
    {
        Commit = commit;
        SubjectParts = TicketLinkResolver.Split(commit.Subject, ticketLinks ?? TicketLinkSettingsModel.Disabled);
    }

    public CommitInfoModel Commit { get; }
    public IReadOnlyList<CommitSubjectPartModel> SubjectParts { get; }
}
