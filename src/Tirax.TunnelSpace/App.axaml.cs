using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Serilog;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.ViewModels;
using Tirax.TunnelSpace.Views;

namespace Tirax.TunnelSpace;

public class App : Application
{
    readonly ServiceProviderEff sp;

    public App() : this(default) { }

    public App(ServiceProviderEff sp) {
        this.sp = sp;
        Init = from vm in sp.GetRequiredService<MainWindowViewModel>()
               from _ in Eff(() => {
                   if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                       desktop.MainWindow = new MainWindow {DataContext = vm};
                   return unit;
               })
               select unit;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        var init = from logger in sp.GetRequiredService<ILogger>()
                   from _1 in Ssh.Initialize(logger)
                   from _2 in Init
                   select unit;
        init.Run().ThrowIfFail();

        base.OnFrameworkInitializationCompleted();
    }

    Eff<Unit> Init { get; }
}