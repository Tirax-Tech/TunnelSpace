using Akka.Actor;
using Akka.Configuration;
using Tirax.TunnelSpace.EffHelpers;

namespace Tirax.TunnelSpace.Services;

public static class AkkaService
{
    const string ConfigHocon = @"
akka.actor.ask-timeout = 10s
";
    public static Eff<ActorSystem> System { get; } =
        (from config in Eff(() => ConfigurationFactory.ParseString(ConfigHocon))
         from system in ActorEff.CreateActorSystem("TiraxTunnelSpace", config)
         select system
        ).Memo();
}