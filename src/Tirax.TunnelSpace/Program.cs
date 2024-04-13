using Avalonia;
using Avalonia.ReactiveUI;
using System;
using Microsoft.Extensions.DependencyInjection;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Flows;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace;

sealed class Program
{
    static Eff<Unit> RunMainApp(IAppMainWindow mainVm) =>
        (
            from services in TunnelSpaceServices.Setup(new ServiceCollection())
            let provider = services.AddSingleton<IAppMainWindow>(mainVm)
                                   .AddSingleton<IMainProgram, MainProgram>()
                                   .AddSingleton<IConnectionSelectionFlow, ConnectionSelectionFlow>()
                                   .BuildServiceProvider()
            from main in provider.GetRequiredServiceEff<IMainProgram>()
            from vm in main.Start
            from __ in mainVm.PushView(vm)
            select unit
        ).ToBackground();

    static Eff<AppBuilder> BuildApp(Func<IAppMainWindow, Eff<Unit>> init) =>
        // Application must be created inside the Configure function!
        Eff(() => AppBuilder.Configure(() => new App(init))
                            .UsePlatformDetect()
                            .WithInterFont()
                            .LogToTrace()
                            .UseReactiveUI());

    static Eff<AppBuilder> BuildMainApp() =>
        BuildApp(RunMainApp);

    // Avalonia configuration, don't remove; also used by visual designer.
    // ReSharper disable once UnusedMember.Global
    public static AppBuilder BuildAvaloniaApp() =>
        BuildMainApp().Run().ThrowIfFail();

    static Eff<int> Run(Func<IAppMainWindow, Eff<Unit>> starter, Seq<string> args) =>
        from app in BuildApp(starter)
        from ret in Eff(() => app.StartWithClassicDesktopLifetime(args.ToArray()))
        select ret;

    static Eff<int> MainEff(Seq<string> args) =>
        Run(RunMainApp, args);

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args) =>
        MainEff(Seq(args)).Run().ThrowIfFail();
}
