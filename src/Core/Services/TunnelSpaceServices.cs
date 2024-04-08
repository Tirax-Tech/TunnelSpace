using Microsoft.Extensions.DependencyInjection;
using Tirax.TunnelSpace.EffHelpers;

namespace Tirax.TunnelSpace.Services;

public static class TunnelSpaceServices
{
    public static Eff<IServiceCollection> Setup(IServiceCollection services) =>
        from tp in SuccessEff(TimeProviderEff.System)
        from now in tp.LocalNow
        from logger in LogSetup.Setup(now)
        select services.AddSingleton(tp)
                       .AddSingleton(logger)
                       .AddSingleton<ITunnelConfigStorage, TunnelConfigStorage>()
                       .AddSingleton<IUniqueId, UniqueId>();
}