using Avalonia;
using Avalonia.ReactiveUI;
using System;
using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Flows;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.Services.Akka;
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
        Eff(() => AppBuilder.Configure(() => container.GetRequiredService<App>().Run().ThrowIfFail())
                            .UsePlatformDetect()
                            .WithInterFont()
                            .LogToTrace()
                            .UseReactiveUI());

    static readonly Eff<ServiceProviderEff> Container =
        from services in TunnelSpaceServices.Setup(new ServiceCollection())
        let provider = services.AddSingleton<App>()
                               .AddSingleton<ActorSystem>(_ => AkkaService.Initialize.Run().ThrowIfFail())
                               .AddSingleton<ISshManager, SshManager>()
                               .AddSingleton<IAppMainWindow, MainWindowViewModel>()
                               .AddSingleton<ServiceProviderEff>()
                               .AddSingleton<IMainProgram, MainProgram>()
                               .AddSingleton<IConnectionSelectionFlow, ConnectionSelectionFlow>()
                               .BuildServiceProvider()
        select provider.GetRequiredService<ServiceProviderEff>();
}
