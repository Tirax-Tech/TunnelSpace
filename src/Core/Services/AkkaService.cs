using Akka.Actor;
using Akka.Configuration;
using Tirax.TunnelSpace.EffHelpers;

namespace Tirax.TunnelSpace.Services;

public static class AkkaService
{
    public static Eff<ActorSystem> System { get; } =
        (from hocon in FileIoEff.ReadAllText("config.hocon")
         from config in Eff(() => ConfigurationFactory.ParseString(hocon))
         from system in ActorEff.CreateActorSystem("TiraxTunnelSpace", config)
         select system
        ).Memo();
}