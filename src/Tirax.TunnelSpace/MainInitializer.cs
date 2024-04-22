using System;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Tirax.TunnelSpace.Flows;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace;

sealed class MainInitializer : IAppInit
{
    Func<Aff<Unit>> shutdown = () => unitAff;
    ILogger logger = default!;

    public Aff<Unit> Start(IAppMainWindow vm) =>
        Aff(async () => {
                     var provider = CreateDiContainer(vm);
                     var akka = provider.GetRequiredService<IAkka>();
                     logger = provider.GetRequiredService<ILogger>();

                     Ssh.Initialize(logger);

                     shutdown = () => from _1 in akka.Shutdown() | @catchOf<Error>(e => {
                                                                                       logger.Error(e, "Error during shutdown");
                                                                                       return e;
                                                                                   })
                                      from _2 in eff(() => logger.Information("Shutdown completed"))
                                      select unit;

                     var storage = provider.GetRequiredService<ITunnelConfigStorage>();
                     var init = from _1 in storage.Init()
                                from _2 in akka.Init()
                                let main = provider.GetRequiredService<IMainProgram>()
                                from _3 in main.Start()
                                select unit;
                     return await init.RunUnit();
                 }) | @catchOf(DisplayError(vm));

    public Aff<Unit> Shutdown() =>
        shutdown();

    static ServiceProvider CreateDiContainer(IAppMainWindow mainVm) =>
        TunnelSpaceServices.Setup(new ServiceCollection())
                           .AddSingleton(mainVm)
                           .AddSingleton<IMainProgram, MainProgram>()
                           .AddSingleton<IConnectionSelectionFlow, ConnectionSelectionFlow>()
                           .BuildServiceProvider();

    static Func<Error, Unit> DisplayError(IAppMainWindow mainVm) =>
        e => mainVm.Reset(new LoadingScreenViewModel(e.Exception.IfSome(out var err) ? err.ToString() : e.Message));
}