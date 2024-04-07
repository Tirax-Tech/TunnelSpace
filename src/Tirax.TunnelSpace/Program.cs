using Avalonia;
using Avalonia.ReactiveUI;
using System;
using Microsoft.Extensions.DependencyInjection;
using Tirax.TunnelSpace.EffHelpers;
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
    public static AppBuilder BuildAvaloniaApp()
        => BuildApp().Run().ThrowIfFail();

    static Eff<AppBuilder> BuildApp() =>
        from container in Container
        from app in BuildApp(container)
        select app;

    static Eff<AppBuilder> BuildApp(ServiceProviderEff container) =>
        Eff(() => AppBuilder.Configure(() => new App(container))
                            .UsePlatformDetect()
                            .WithInterFont()
                            .LogToTrace()
                            .UseReactiveUI());

    static readonly Eff<ServiceProviderEff> Container =
        from services in TunnelSpaceServices.Setup(new ServiceCollection())
        let provider = services
                       .AddTransient<MainWindowViewModel>()
                       .AddTransient<ConnectionSelectionViewModel>()
                       .AddTransient<TunnelConfigViewModel>()
                       .AddSingleton<ServiceProviderEff>()
                       .BuildServiceProvider()
        select provider.GetRequiredService<ServiceProviderEff>();
}
