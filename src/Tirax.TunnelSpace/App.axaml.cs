using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Tirax.TunnelSpace.ViewModels;
using Tirax.TunnelSpace.Views;

namespace Tirax.TunnelSpace;

interface IAppInit
{
    OutcomeAsync<Unit> Start(IAppMainWindow vm);
    OutcomeAsync<Unit> Shutdown();
}

class App(IAppInit initializer) : Application
{
    public static readonly IAppInit DoNothing = new DumpInit();

    [DesignOnly(true)]
    public App() : this(DoNothing) { }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            var vm = new MainWindowViewModel();
            desktop.MainWindow = new MainWindow { DataContext = vm };

            Task.Run(async () => await initializer.Start(vm));
        }

        base.OnFrameworkInitializationCompleted();
    }

    sealed class DumpInit : IAppInit
    {
        public OutcomeAsync<Unit> Start(IAppMainWindow vm) => unit;

        public OutcomeAsync<Unit> Shutdown() => unit;
    }
}