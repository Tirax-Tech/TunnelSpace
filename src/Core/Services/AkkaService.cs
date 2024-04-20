﻿using Akka.Actor;
using Akka.Actor.Setup;
using Akka.Configuration;
using Akka.DependencyInjection;
using RZ.Foundation.Akka;
using Serilog;
using Tirax.TunnelSpace.Helpers;
using Tirax.TunnelSpace.Services.Akka;

namespace Tirax.TunnelSpace.Services;

public interface IAkka
{
    ActorSystem System { get; }

    SshManager SshManager { get; }

    Outcome<Unit>      Init();
    OutcomeAsync<Unit> Shutdown();
}

public sealed class AkkaService(ILogger logger, IUniqueId uniqueId, IServiceProvider sp) : IAkka
{
    const string ConfigHocon = @"
akka.actor.ask-timeout = 10s
";

    readonly Lazy<ActorSystem> sys = new(() => InitSystem(sp));
    SshManager? sshManager;

    public ActorSystem System => sys.Value;
    public SshManager SshManager => sshManager ?? throw new InvalidOperationException("Akka not initialized");

    public Outcome<Unit> Init() {
        var manager = System.CreateActor<SshManagerActor>("ssh-manager");
        sshManager = new(uniqueId, manager);
        logger.Information("Akka initialized");
        return unit;
    }

    public OutcomeAsync<Unit> Shutdown() =>
        TryAsync(async () => await System.CoordinatedShutdown()).ToOutcome();

    static ActorSystem InitSystem(IServiceProvider sp) {
        var config = BootstrapSetup.Create().WithConfig(ConfigurationFactory.ParseString(ConfigHocon));
        var diSetup = DependencyResolverSetup.Create(sp);
        var setup = ActorSystemSetup.Create(config, diSetup);
        return ActorSystem.Create("TiraxTunnelSpace", setup);
    }
}