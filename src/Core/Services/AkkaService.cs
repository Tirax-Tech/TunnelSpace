using Akka.Actor;
using Akka.Actor.Setup;
using Akka.Configuration;
using Akka.DependencyInjection;
using Tirax.TunnelSpace.EffHelpers;
using Tirax.TunnelSpace.Services.Akka;

namespace Tirax.TunnelSpace.Services;

public interface IAkka
{
    ActorSystem System { get; }

    SshManager SshManager { get; }

    Aff<Unit> Init { get; }
    Aff<Unit> Shutdown { get; }
}

public sealed class AkkaService : IAkka
{
    const string ConfigHocon = @"
akka.actor.ask-timeout = 10s
";
    static Eff<ActorSystem> InitSystem(IServiceProvider sp) =>
        from config in Eff(() => BootstrapSetup.Create().WithConfig(ConfigurationFactory.ParseString(ConfigHocon)))
        let diSetup = DependencyResolverSetup.Create(sp)
        let setup = ActorSystemSetup.Create(config, diSetup)
        let system = ActorSystem.Create("TiraxTunnelSpace", setup)
        select system;

    public AkkaService(IServiceProvider sp) {
        var sys = (from system in InitSystem(sp)
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