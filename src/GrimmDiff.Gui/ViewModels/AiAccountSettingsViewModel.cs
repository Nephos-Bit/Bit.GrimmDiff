using GrimmDiff.Gui.Services;
using GrimmDiff.Gui.Commands;
namespace GrimmDiff.Gui.ViewModels;

public sealed class AiAccountSettingsViewModel : SettingsPageViewModelBase
{
    private readonly CodexAccountViewModel _account;
    public AiAccountSettingsViewModel(PreferencesStateService preferences, CodexAccountViewModel account) :
        base(preferences, "ai-account", "Ai", "Account", 0)
    {
        _account = account;
        _account.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(CodexAccountViewModel.StatusText) or "") OnPropertyChanged(nameof(StatusText));
            if (args.PropertyName is nameof(CodexAccountViewModel.AccountText) or "") OnPropertyChanged(nameof(AccountText));
            if (args.PropertyName is nameof(CodexAccountViewModel.WeeklyQuotaText) or "") OnPropertyChanged(nameof(WeeklyQuotaText));
            if (args.PropertyName is nameof(CodexAccountViewModel.WeeklyQuotaToolTip) or "") OnPropertyChanged(nameof(WeeklyQuotaToolTip));
        };
    }
    public string StatusText => _account.StatusText;
    public string AccountText => _account.AccountText;
    public string WeeklyQuotaText => _account.WeeklyQuotaText;
    public string WeeklyQuotaToolTip => _account.WeeklyQuotaToolTip;
    public AsyncCommand LoginCommand => _account.LoginCommand;
}
