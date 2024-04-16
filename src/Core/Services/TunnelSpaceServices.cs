using Microsoft.Extensions.DependencyInjection;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Services.Akka;

namespace Tirax.TunnelSpace.Services;

public static class TunnelSpaceServices
{
    public static Eff<IServiceCollection> Setup(IServiceCollection services) =>
        from tp in SuccessEff(TimeProviderEff.System)
        from now in tp.LocalNow
        from logger in LogSetup.Setup
        select services.AddSingleton(tp)
                       .AddSingleton(logger)
                       .AddSingleton<IAkka, AkkaService>()
                       .AddSingleton<ISshManager>(sp => sp.GetRequiredService<IAkka>().SshManager)
                       .AddSingleton<ITunnelConfigStorage, TunnelConfigStorage>()
                       .AddSingleton<IUniqueId, UniqueId>();
}