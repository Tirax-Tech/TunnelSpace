using Microsoft.Extensions.DependencyInjection;
using Tirax.TunnelSpace.Helpers;
using Tirax.TunnelSpace.Services.Akka;

namespace Tirax.TunnelSpace.Services;

public static class TunnelSpaceServices
{
    public static IServiceCollection Setup(IServiceCollection services) =>
        services.AddSingleton(TimeProvider.System)
                .AddSingleton(LogSetup.Setup())
                .AddSingleton<IAkka, AkkaService>()
                .AddSingleton<ISshManager>(sp => sp.GetRequiredService<IAkka>().SshManager)
                .AddSingleton<ITunnelConfigStorage, TunnelConfigStorage>()
                .AddSingleton<IUniqueId, UniqueId>();
}