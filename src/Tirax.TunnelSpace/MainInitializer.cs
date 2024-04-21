using System;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Tirax.TunnelSpace.Flows;
using Tirax.TunnelSpace.Services;
using Tirax.TunnelSpace.ViewModels;

namespace Tirax.TunnelSpace;

sealed class MainInitializer : IAppInit
{
    Func<OutcomeAsync<Unit>> shutdown = () => unit;
    ILogger logger = default!;

    public OutcomeAsync<Unit> Start(IAppMainWindow vm) =>
        TryCatch(async () => {
                     var provider = CreateDiContainer(vm);
                     var akka = provider.GetRequiredService<IAkka>();
                     logger = provider.GetRequiredService<ILogger>();

                     Ssh.Initialize(logger);

                     shutdown = () => akka.Shutdown()
                                    | @outcomeFailed(e => logger.Error(e, "Error during shutdown"))
                                    | @do<Unit>(_ => logger.Information("Shutdown completed"));

                     var storage = provider.GetRequiredService<ITunnelConfigStorage>();
                     var init = await (from _1 in storage.Init()
                                       from _2 in akka.Init()
                                       let main = provider.GetRequiredService<IMainProgram>()
                                       from _3 in main.Start()
                                       select unit);
                     return init.Unwrap();
                 }) | failDo(DisplayError(vm));

    public OutcomeAsync<Unit> Shutdown() =>
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