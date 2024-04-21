using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Threading.Tasks;
using LanguageExt.UnitsOfMeasure;

namespace Tirax.TunnelSpace;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args) {
        var initializer = new MainInitializer();
        return BuildApp(initializer)
              .StartWithClassicDesktopLifetime(args)
              .SideEffect(_ => Task.Run(initializer.Shutdown)
                                   .Wait(30.Seconds()));
    }

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
}