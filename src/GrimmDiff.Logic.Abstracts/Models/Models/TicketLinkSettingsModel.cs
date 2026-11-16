namespace GrimmDiff.Logic.Abstracts;

public sealed record TicketLinkSettingsModel(TicketLinkProviderModel Provider, string? BaseUrl)
{
    public static TicketLinkSettingsModel Disabled { get; } = new(TicketLinkProviderModel.Disabled, null);
}
