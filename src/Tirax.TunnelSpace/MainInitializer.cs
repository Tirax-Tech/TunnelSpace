using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Tirax.TunnelSpace.Flows;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace;

sealed class MainInitializer : IAppInit
{
    ILogger logger = null!;
    IServiceProvider? serviceProvider;

    public IServiceProvider BuilderServices() =>
        serviceProvider =
            TunnelSpaceServices.Setup(new ServiceCollection())
                               .AddSingleton<MainWindowViewModel>()

                               .AddSingleton<IAppMainWindow>(sp => sp.GetRequiredService<MainWindowViewModel>())
                               .AddSingleton<IMainProgram, MainProgram>()
                               .AddSingleton<IConnectionSelectionFlow, ConnectionSelectionFlow>()
                               .BuildServiceProvider();

    public async Task Start() {
        try {
            var provider = serviceProvider ??= BuilderServices();
            var akka = provider.GetRequiredService<IAkka>();
            logger = provider.GetRequiredService<ILogger>();

            Ssh.Initialize(logger);

            var storage = provider.GetRequiredService<ITunnelConfigStorage>();
            await storage.Init();
            akka.Init();
            var main = provider.GetRequiredService<IMainProgram>();
            await main.Start();
        }
        catch(Exception e){
            await DisplayError(serviceProvider!.GetRequiredService<IAppMainWindow>())(e);
        }
    }

    public async Task Shutdown() {
            var akka = serviceProvider!.GetRequiredService<IAkka>();
        try{
            await akka.Shutdown();
            logger.Information("Akka shutdown completed");
        }
        catch (Exception e){
            logger.Error(e, "Error during akka shutdown");
        }
    }

    static Func<Error, ValueTask<Unit>> DisplayError(IAppMainWindow mainVm) =>
        e => mainVm.Reset(new LoadingScreenViewModel(e.Exception.IfSome(out var err) ? err.ToString() : e.Message));
}