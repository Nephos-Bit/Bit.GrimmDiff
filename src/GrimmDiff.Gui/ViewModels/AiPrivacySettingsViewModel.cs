using GrimmDiff.Gui.Services;
using GrimmDiff.Gui.Commands;
namespace GrimmDiff.Gui.ViewModels;

public sealed class AiPrivacySettingsViewModel : SettingsPageViewModelBase
{
    private readonly RepositoryWorkspaceViewModel _repository;
    public AiPrivacySettingsViewModel(PreferencesStateService preferences, RepositoryWorkspaceViewModel repository,
        AiConsentService consent) : base(preferences, "ai-privacy", "Ai", "PrivacyAndApprovals", 1)
    {
        _repository = repository;
        ShowRiskNoticeCommand = new AsyncCommand(() => { consent.ShowRiskNotice(); return Task.CompletedTask; });
        RevokeApprovalCommand = new AsyncCommand(() =>
        {
            consent.Revoke(repository.RepositoryPath);
            return Task.CompletedTask;
        }, () => repository.HasRepository);
        repository.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(RepositoryWorkspaceViewModel.RepositoryPath))
            {
                OnPropertyChanged(nameof(HasRepository));
                RevokeApprovalCommand.RaiseCanExecuteChanged();
            }
        };
    }
    public AsyncCommand ShowRiskNoticeCommand { get; }
    public AsyncCommand RevokeApprovalCommand { get; }
    public bool HasRepository => _repository.HasRepository;
}
