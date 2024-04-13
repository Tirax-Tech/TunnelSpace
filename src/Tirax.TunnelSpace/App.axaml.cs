using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Tirax.TunnelSpace.ViewModels;
using Tirax.TunnelSpace.Views;

namespace Tirax.TunnelSpace;

public class App(Func<IAppMainWindow, Eff<Unit>> init) : Application
{
    [DesignOnly(true)]
    public App() : this(_ => unitEff) {}

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            var vm = new MainWindowViewModel();
            desktop.MainWindow = new MainWindow { DataContext = vm };
            init(vm).RunUnit();
        }

        base.OnFrameworkInitializationCompleted();
    }
}