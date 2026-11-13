using GrimmDiff.Data;
using GrimmDiff.Logic;
using Microsoft.Extensions.DependencyInjection;
using Velopack;

namespace GrimmDiff.Gui;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _services;

    [STAThread]
    private static void Main()
    {
        VelopackApp.Build().SetAutoApplyOnStartup(false).Run();
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();

        var view = _services.GetRequiredService<MainView>();
        MainWindow = view;
        view.Closed += (_, _) => _services.Dispose();
        view.Show();
        Dispatcher.BeginInvoke(async () =>
        {
            var viewModel = _services.GetRequiredService<MainViewModel>();
            await viewModel.InitializeAsync();
            await viewModel.CheckForUpdatesOnStartupAsync();
        });
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<GitCliRepositoryService>();
        services.AddSingleton<GitCliRepositoryCommandsAdapter>();
        services.AddSingleton<IRepositoryAvailabilityPort, GitCliRepositoryAvailabilityAdapter>();
        services.AddSingleton<IRepositoryReaderPort, GitCliRepositoryReaderAdapter>();
        services.AddSingleton<IRepositoryCommandsPort>(provider => provider.GetRequiredService<GitCliRepositoryCommandsAdapter>());
        services.AddSingleton<IRepositoryClonerPort>(provider => provider.GetRequiredService<GitCliRepositoryCommandsAdapter>());
        services.AddSingleton<IRepositoryDiffReaderPort, GitCliDiffReaderAdapter>();
        services.AddSingleton<ICodexSessionClientPort, CodexCliClientAdapter>();
        services.AddSingleton<ICodexStructuredRunnerPort>(provider =>
            (CodexCliClientAdapter)provider.GetRequiredService<ICodexSessionClientPort>());
        services.AddSingleton<LocalHeuristicDiffAnalysisService>();
        services.AddSingleton<RepositoryWorkspaceService>();
        services.AddSingleton<DiffAnalysisService>(provider => new DiffAnalysisService(
            provider.GetRequiredService<IRepositoryDiffReaderPort>(),
            new CodexCliDiffAnalysisAdapter(provider.GetRequiredService<ICodexStructuredRunnerPort>(),
                provider.GetRequiredService<LocalHeuristicDiffAnalysisService>()),
            provider.GetRequiredService<LocalHeuristicDiffAnalysisService>()));
        services.AddSingleton<CodexSessionService>();
        services.AddSingleton<PreferencesService>(provider =>
            new PreferencesService(new JsonAppPreferencesAdapter()));
        services.AddSingleton<IUserDialogService, WpfUserDialogService>();
        services.AddSingleton<IExternalNavigationService, ShellExternalNavigationService>();
        services.AddSingleton<IRepositoryChangeMonitorService, FileSystemRepositoryChangeMonitorService>();
        services.AddSingleton<IApplicationUpdateService, VelopackUpdateService>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<MainView>();
    }
}
