using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.ViewModels;
using Tirax.TunnelSpace.Views;

namespace Tirax.TunnelSpace;

sealed record AppInit(Aff<Unit> Start, Aff<Unit> Shutdown)
{
    public static readonly AppInit DoNothing = new(unitAff, unitAff);
}

class App(TaskCompletionSource<Aff<Unit>> initialized, Func<IAppMainWindow, Eff<AppInit>> init) : Application
{
    [DesignOnly(true)]
    public App() : this(new(), _ => SuccessEff(AppInit.DoNothing)) {}

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            var vm = new MainWindowViewModel();
            desktop.MainWindow = new MainWindow { DataContext = vm };

            var run = from appInit in init(vm)
                      from _1 in eff(() => initialized.SetResult(appInit.Shutdown))
                      from _2 in appInit.Start
                      select unit;
            run.RunIgnore();
        }

        base.OnFrameworkInitializationCompleted();
    }
}