using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Tirax.TunnelSpace.Views;

namespace Tirax.TunnelSpace;

interface IAppInit
{
    IServiceProvider   BuilderServices();
    OutcomeAsync<Unit> Start();
    OutcomeAsync<Unit> Shutdown();
}

class App(IServiceProvider sp, IAppInit initializer) : Application
{
    public static readonly IAppInit DoNothing = new DumpInit();

    [DesignOnly(true)]
    public App() : this(DoNothing.BuilderServices(), DoNothing) { }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) {
            desktop.MainWindow = ActivatorUtilities.CreateInstance<MainWindow>(sp);

            Task.Run(async () => await initializer.Start());
        }

        base.OnFrameworkInitializationCompleted();
    }

    sealed class DumpInit : IAppInit
    {
        public IServiceProvider BuilderServices() =>
            new ServiceCollection().BuildServiceProvider();

        public OutcomeAsync<Unit> Start() => unit;

        public OutcomeAsync<Unit> Shutdown() => unit;
    }
}