using Avalonia;
using Avalonia.ReactiveUI;
using System;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Tirax.TunnelSpace.EffHelpers;

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
    public static AppBuilder BuildAvaloniaApp()
        => BuildApp().Run().ThrowIfFail();

    static Eff<AppBuilder> BuildApp() =>
        Eff(() => AppBuilder.Configure(() => new App(ServiceProviderEff.Call(Container)))
                            .UsePlatformDetect()
                            .WithInterFont()
                            .LogToTrace()
                            .UseReactiveUI());

    static readonly IServiceProvider Container =
        new ServiceCollection()
            .AddSingleton(SetupLog())
            .BuildServiceProvider();

    static ILogger SetupLog() =>
        new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
}
