using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RZ.Foundation.Functional;
using Serilog;
using Tirax.TunnelSpace.Flows;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace;

sealed class MainInitializer : IAppInit
{
    Func<OutcomeAsync<Unit>> shutdown = () => FailedOutcomeAsync<Unit>(StandardErrors.Unexpected);
    ILogger logger = default!;
    IServiceProvider? serviceProvider;

    public IServiceProvider BuilderServices() =>
        serviceProvider =
            TunnelSpaceServices.Setup(new ServiceCollection())
                               .AddSingleton<MainWindowViewModel>()

                               .AddSingleton<IAppMainWindow>(sp => sp.GetRequiredService<MainWindowViewModel>())
                               .AddSingleton<IMainProgram, MainProgram>()
                               .AddSingleton<IConnectionSelectionFlow, ConnectionSelectionFlow>()
                               .BuildServiceProvider();

    public OutcomeAsync<Unit> Start() =>
        TryCatch(async () => {
            var provider = serviceProvider ?? BuilderServices();
            var akka = provider.GetRequiredService<IAkka>();
            logger = provider.GetRequiredService<ILogger>();

            Ssh.Initialize(logger);

            shutdown = () => akka.Shutdown();

            var storage = provider.GetRequiredService<ITunnelConfigStorage>();
            var init = await (from _1 in storage.Init()
                              from _2 in akka.Init()
                              let main = provider.GetRequiredService<IMainProgram>()
                              from _3 in main.Start()
                              select unit);
            return init.Unwrap();
        }) | failDo(async e => await DisplayError(serviceProvider!.GetRequiredService<IAppMainWindow>())(e));

    public OutcomeAsync<Unit> Shutdown() =>
        shutdown()
      | @ifFail(e => logger.Error(e, "Error during shutdown"))
      | @do<Unit>(_ => logger.Information("Shutdown completed"));

    static Func<Error, ValueTask<Unit>> DisplayError(IAppMainWindow mainVm) =>
        e => mainVm.Reset(new LoadingScreenViewModel(e.Exception.IfSome(out var err) ? err.ToString() : e.Message));
}