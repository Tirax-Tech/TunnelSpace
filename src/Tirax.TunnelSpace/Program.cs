global using static Tirax.TunnelSpace.Effects.Prelude;

using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Flows;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace;

sealed class Program
{
    static Eff<ServiceProvider> CreateDiContainer(IAppMainWindow mainVm) =>
        from services in TunnelSpaceServices.Setup(new ServiceCollection())
        select services.AddSingleton<IAppMainWindow>(mainVm)
                       .AddSingleton<IMainProgram, MainProgram>()
                       .AddSingleton<IConnectionSelectionFlow, ConnectionSelectionFlow>()
                       .BuildServiceProvider();

    static Func<Error, Aff<Unit>> DisplayError(IAppMainWindow mainVm) => e =>
        mainVm.PushView(new LoadingScreenViewModel(e.ToString()));

    static Eff<AppInit> RunMainApp(IAppMainWindow mainVm) =>
        from provider in CreateDiContainer(mainVm)
        from akka in provider.GetRequiredServiceEff<IAkka>()
        from logger in provider.GetRequiredServiceEff<ILogger>()

        let start = from _1 in akka.Init
                    from main in provider.GetRequiredServiceEff<IMainProgram>()
                    from _2 in Aff(async () => (await main.Start()).IfLeft(e => logger.Error(e, "Error during startup")))
                    select unit
        let shutdown = akka.Shutdown

        let displayError = DisplayError(mainVm)
        select new AppInit(start | @catch(displayError),
                           shutdown | @catch(e => logger.ErrorEff(e, "Error during shutdown")));

    static Eff<AppBuilder> BuildApp(TaskCompletionSource<Aff<Unit>> initialized, Func<IAppMainWindow, Eff<AppInit>> init) =>
        // Application must be created inside the Configure function!
        Eff(() => AppBuilder.Configure(() => new App(initialized, init))
                            .UsePlatformDetect()
                            .WithInterFont()
                            .LogToTrace()
                            .UseReactiveUI());

    // Avalonia configuration, don't remove; also used by visual designer.
    // ReSharper disable once UnusedMember.Global
    public static AppBuilder BuildAvaloniaApp() =>
        BuildApp(new(), _ => SuccessEff(AppInit.DoNothing)).Run().ThrowIfFail();

    static Aff<int> Run(Func<IAppMainWindow, Eff<AppInit>> starter, Seq<string> args) =>
        from initialized in SuccessEff(new TaskCompletionSource<Aff<Unit>>())
        from app in BuildApp(initialized, starter)
        from ret in Eff(() => app.StartWithClassicDesktopLifetime(args.ToArray()))
        from shutdown in Aff(async () => await initialized.Task)
        from _ in shutdown
        select ret;

    static Aff<int> MainEff(Seq<string> args) =>
        Run(RunMainApp, args);

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task<int> Main(string[] args) =>
        (await MainEff(Seq(args)).Run()).ThrowIfFail();
}
