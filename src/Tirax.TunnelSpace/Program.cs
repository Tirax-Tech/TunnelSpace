global using static Tirax.TunnelSpace.Effects.Prelude;
global using Tirax.TunnelSpace.Effects;

using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Flows;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace;

sealed class Program
{
    static Eff<ServiceProvider> CreateDiContainer(AkkaService akka, IAppMainWindow mainVm) =>
        from services in TunnelSpaceServices.Setup(akka, new ServiceCollection())
        select services.AddSingleton<IAppMainWindow>(mainVm)
                       .AddSingleton<IMainProgram, MainProgram>()
                       .AddSingleton<IConnectionSelectionFlow, ConnectionSelectionFlow>()
                       .BuildServiceProvider();

    static Func<IAppMainWindow, Eff<Unit>> RunMainApp(AkkaService akka) => mainVm =>
        (
            from provider in CreateDiContainer(akka, mainVm)
            let start =
                from main in provider.GetRequiredServiceEff<IMainProgram>()
                from vm in main.Start
                from __ in mainVm.PushView(vm)
                select unit
            from _ in start.MatchAff(_ => unitAff,
                                     e => mainVm.PushView(new LoadingScreenViewModel(e.ToString())).Ignore())
            select unit
        ).ToBackground();

    static Eff<AppBuilder> BuildApp(Func<IAppMainWindow, Eff<Unit>> init) =>
        // Application must be created inside the Configure function!
        Eff(() => AppBuilder.Configure(() => new App(init))
                            .UsePlatformDetect()
                            .WithInterFont()
                            .LogToTrace()
                            .UseReactiveUI());

    // Avalonia configuration, don't remove; also used by visual designer.
    // ReSharper disable once UnusedMember.Global
    public static AppBuilder BuildAvaloniaApp() =>
        BuildApp(_ => unitEff).Run().ThrowIfFail();

    static Eff<int> Run(Func<IAppMainWindow, Eff<Unit>> starter, Seq<string> args) =>
        from app in BuildApp(starter)
        from ret in Eff(() => app.StartWithClassicDesktopLifetime(args.ToArray()))
        select ret;

    static Aff<int> MainEff(Seq<string> args) =>
        from akka in Eff(() => new AkkaService())
        from __1 in akka.Init
        from ret in Run(RunMainApp(akka), args)
        from __2 in akka.Shutdown
        select ret;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task<int> Main(string[] args) =>
        (await MainEff(Seq(args)).Run()).ThrowIfFail();
}
