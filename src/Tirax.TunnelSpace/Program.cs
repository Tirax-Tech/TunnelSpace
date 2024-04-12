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
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() {
        return BuildApp().Run().ThrowIfFail();
    }

    static Eff<AppBuilder> BuildApp() =>
        from services in TunnelSpaceServices.Setup(new ServiceCollection())
        let provider = services.AddSingleton<App>()
                               .AddSingleton<IAppMainWindow, MainWindowViewModel>()
                               .AddSingleton<IMainProgram, MainProgram>()
                               .AddSingleton<IConnectionSelectionFlow, ConnectionSelectionFlow>()
                               .BuildServiceProvider()
        from app in BuildApp(provider)
        select app;

    static Eff<AppBuilder> BuildApp(IServiceProvider container) =>
        // Application must be created inside the Configure function!
        Eff(() => AppBuilder.Configure(() => container.GetRequiredServiceEff<App>().Run().ThrowIfFail())
                            .UsePlatformDetect()
                            .WithInterFont()
                            .LogToTrace()
                            .UseReactiveUI());
}
