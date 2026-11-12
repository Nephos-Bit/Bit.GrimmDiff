using Velopack;

namespace GrimmDiff.App;

public partial class App : System.Windows.Application
{
    [STAThread]
    private static void Main()
    {
        VelopackApp.Build().SetAutoApplyOnStartup(false).Run();
        var app = new App();
        app.InitializeComponent();
        app.Run(new MainWindow());
    }
}