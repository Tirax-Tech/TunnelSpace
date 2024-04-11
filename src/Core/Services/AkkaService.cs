using Akka.Actor;
using Akka.Configuration;
using Tirax.TunnelSpace.EffHelpers;

namespace Tirax.TunnelSpace.Services;

public sealed class AkkaService
{
    public static Eff<ActorSystem> Initialize { get; } =
        from hocon in FileIoEff.ReadAllText("config.hocon")
        from config in Eff(() => ConfigurationFactory.ParseString(hocon))
        from system in ActorEff.CreateActorSystem("TiraxTunnelSpace", config)
        select system;
}