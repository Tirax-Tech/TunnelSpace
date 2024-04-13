using Akka.Actor;
using Akka.Configuration;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Services.Akka;

namespace Tirax.TunnelSpace.Services;

public interface IAkka
{
    ActorSystem System { get; }

    SshManager SshManager { get; }
}

public interface IHostedServiceEff
{
    Aff<Unit> Init { get; }
    Aff<Unit> Shutdown { get; }
}

public sealed class AkkaService : IHostedServiceEff, IAkka
{
    const string ConfigHocon = @"
akka.actor.ask-timeout = 10s
";
    static Eff<ActorSystem> InitSystem { get; } =
        from config in Eff(() => ConfigurationFactory.ParseString(ConfigHocon))
        from system in ActorEff.CreateActorSystem("TiraxTunnelSpace", config)
        select system;

    public AkkaService() {
        var sys = (from system in InitSystem
                   from manager in system.CreateActor<SshManagerActor>(() => new SshManagerActor(), "ssh-manager")
                   from ___ in eff(() => {
                                       System = system;
                                       SshManager = new(manager);
                                   })
                   select system).Memo();

        Init = sys.Map(_ => unit);

        Shutdown = sys.Bind(akka => akka.CoordinatedShutdown());
    }

    public Aff<Unit> Init { get; }
    public Aff<Unit> Shutdown { get; }

    public ActorSystem System { get; private set; } = default!;
    public SshManager SshManager { get; private set; } = new(ActorRefs.Nobody);
}