using Akka.Actor;
using Akka.Actor.Setup;
using Akka.Configuration;
using Akka.DependencyInjection;
using Serilog;
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
        var sys = (from logger in sp.GetRequiredServiceEff<ILogger>()
                   from system in InitSystem(sp)
                   from manager in system.CreateActor<SshManagerActor>("ssh-manager")
                   from uniqueId in sp.GetRequiredServiceEff<IUniqueId>()
                   from _1 in eff(() => {
                                      System = system;
                                      SshManager = new(uniqueId, manager);
                                  })
                   from _2 in logger.InformationEff("Akka initialized")
                   select system).Memo();

        Init = sys.Map(_ => unit);

        Shutdown = sys.Bind(akka => akka.CoordinatedShutdown());
    }

    public Aff<Unit> Init { get; }
    public Aff<Unit> Shutdown { get; }

    public ActorSystem System { get; private set; } = default!;
    public SshManager SshManager { get; private set; } = default!;
}