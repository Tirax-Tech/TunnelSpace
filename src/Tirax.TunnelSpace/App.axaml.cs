using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Serilog;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Flows;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace;

public class App : Application
{
    readonly ILogger logger;

    public App(ILogger logger, IAppMainWindow mainWindow, IMainProgram main) {
        this.logger = logger;
        Init = from view in mainWindow.CreateView
               from _1 in eff(() => {
                                  if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
                                      desktop.MainWindow = view;
                                  }
                              })
               select unit;
        InitAsync = from vm in main.Start
                    from _2 in mainWindow.Replace(vm)
                    select unit;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        var init = from _1 in Ssh.Initialize(logger)
                   from _2 in Init.MatchEff(_ => logger.InformationEff("App initialized"),
                                            e => logger.ErrorEff(e, "App initialization failed"))
                   select unit;
        init.RunUnit();
        InitAsync.RunIgnore();

        base.OnFrameworkInitializationCompleted();
    }

    Eff<Unit> Init { get; }
    Aff<Unit> InitAsync { get; }
}