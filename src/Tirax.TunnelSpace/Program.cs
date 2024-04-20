global using static Tirax.TunnelSpace.Effects.Prelude;

using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Tirax.TunnelSpace.Flows;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task<int> Main(string[] args) {
        var initializer = new MainInitializer();
        var app = BuildApp(initializer);
        var ret = app.StartWithClassicDesktopLifetime(args);

        await Task.Run(async () => await initializer.Shutdown() | outcomeFailed(e => Log.Logger.Error(e, "Error during shutdown")));
        return ret;
    }

    static ServiceProvider CreateDiContainer(IAppMainWindow mainVm) =>
        TunnelSpaceServices.Setup(new ServiceCollection())
                           .AddSingleton(mainVm)
                           .AddSingleton<IMainProgram, MainProgram>()
                           .AddSingleton<IConnectionSelectionFlow, ConnectionSelectionFlow>()
                           .BuildServiceProvider();

    static Func<Error, Unit> DisplayError(IAppMainWindow mainVm) =>
        e => mainVm.Reset(new LoadingScreenViewModel(e.Exception.IfSome(out var err) ? err.ToString() : e.Message));

    static AppBuilder BuildApp(IAppInit init) =>
        // Application must be created inside the Configure function!
        AppBuilder.Configure(() => new App(init))
                  .UsePlatformDetect()
                  .WithInterFont()
                  .LogToTrace()
                  .UseReactiveUI();

    // Avalonia configuration, don't remove; also used by visual designer.
    // ReSharper disable once UnusedMember.Global
    public static AppBuilder BuildAvaloniaApp() =>
        BuildApp(App.DoNothing);

    sealed class MainInitializer : IAppInit
    {
        Func<Task<Unit>> shutdown = () => Task.FromResult(unit);

        public OutcomeAsync<Unit> Start(IAppMainWindow vm) =>
            TryAsync(async () => {
                         var provider = CreateDiContainer(vm);
                         var akka = provider.GetRequiredService<IAkka>();
                         var logger = provider.GetRequiredService<ILogger>();

                         shutdown = async () => {
                                        ___(await akka.Shutdown() | outcomeFailed(e => logger.Error(e, "Error during shutdown")));
                                        return unit;
                                    };

                         var init = await (from _1 in akka.Init()
                                           let main = provider.GetRequiredService<IMainProgram>()
                                           from _2 in main.Start()
                                           select unit);
                         return init.Unwrap();
                     }).ToOutcome() | catchAndReraise(DisplayError(vm));

        public OutcomeAsync<Unit> Shutdown() =>
            TryAsync(shutdown).ToOutcome();
    }
}
