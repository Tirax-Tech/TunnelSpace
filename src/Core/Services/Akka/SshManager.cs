using System.Reactive.Subjects;
using Akka.Actor;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.EffHelpers;

namespace Tirax.TunnelSpace.Services.Akka;

public sealed record TunnelState(Guid TunnelId, bool IsRunning);

public interface ISshManager
{
    Aff<TunnelConfig[]> All { get; }

    Aff<Unit> AddTunnel(TunnelConfig config);
    Aff<Unit> UpdateTunnel(TunnelConfig config);
    Aff<Unit> DeleteTunnel(Guid tunnelId);

    Aff<Unit> StartTunnel(Guid tunnelId);
    Aff<Unit> StopTunnel(Guid tunnelId);

    IObservable<Change<TunnelConfig>> Changes { get; }
    IObservable<TunnelState> TunnelRunningStateChanges { get; }
}

public sealed class SshManager : ISshManager
{
    public SshManager(IUniqueId uniqueId, IActorRef manager) {
        this.manager = manager;

        All = manager.AskEff<TunnelConfig[]>(nameof(All));

        Changes = manager.CreateObservable<Change<TunnelConfig>>(uniqueId);
        TunnelRunningStateChanges = manager.CreateObservable<TunnelState>(uniqueId);
    }

    public Aff<TunnelConfig[]> All { get; }

    public Aff<Unit> AddTunnel(TunnelConfig config) => manager.AskEff<Unit>((nameof(AddTunnel), config));
    public Aff<Unit> UpdateTunnel(TunnelConfig config) => manager.AskEff<Unit>((nameof(UpdateTunnel), config));

    public Aff<ISshController> CreateSshController(TunnelConfig config) =>
        from actor in manager.AskEff<IActorRef>((nameof(CreateSshController), config))
        select (ISshController) new SshControllerWrapper(actor);

    public Aff<Unit> DeleteTunnel(Guid tunnelId) => manager.AskEff<Unit>((nameof(DeleteTunnel), tunnelId));
    public Aff<Unit> StartTunnel(Guid tunnelId)  => manager.AskEff<Unit>((nameof(StartTunnel), tunnelId));
    public Aff<Unit> StopTunnel(Guid tunnelId)   => manager.AskEff<Unit>((nameof(StopTunnel), tunnelId));

    public IObservable<Change<TunnelConfig>> Changes { get; }
    public IObservable<TunnelState> TunnelRunningStateChanges { get; }

    readonly IActorRef manager;
}

public sealed class SshManagerActor : UntypedActorEff, IWithUnboundedStash
{
    sealed record SshTunnelItem(TunnelConfig Config, Option<IActorRef> Controller = default);

    readonly ITunnelConfigStorage storage;
    Dictionary<Guid, SshTunnelItem> tunnels = new();
    readonly Dictionary<Guid, IDisposable> observableDisposables = new();
    readonly Subject<Change<TunnelConfig>> changes = new();
    readonly Subject<TunnelState> tunnelRunningStateChanges = new();

    public SshManagerActor(ITunnelConfigStorage storage) {
        this.storage = storage;
        PreStartEff = from _1 in BecomeStacked(m => m switch
                                                    {
                                                        Fin<TunnelConfig[]> data => from _1 in SetTunnels(data.ThrowIfFail())
                                                                                    from _2 in UnbecomeStacked
                                                                                    from _3 in eff(Stash.UnstashAll)
                                                                                    select unit,
                                                        _ => eff(Stash.Stash)
                                                    })
                      select unit;
        Self.PipeFrom(from data in storage.All select data.ToArray());
        return;

        Eff<Unit> SetTunnels(TunnelConfig[] configs) =>
            eff(() => tunnels = (from config in configs
                                 select KeyValuePair.Create(config.Id!.Value, new SshTunnelItem(config))
                                ).ToDictionary());
    }

    protected override Eff<Unit> OnReceiveEff(object message) =>
        message switch
        {
            nameof(ISshManager.All) => Sender.Respond(SuccessEff(tunnels.Values.Map(t => t.Config).ToArray())),

            (nameof(ISshManager.DeleteTunnel), Guid tunnelId) => Sender.Respond(from _ in storage.Delete(tunnelId) select unit),

            (nameof(ISshManager.StartTunnel), Guid tunnelId) => StartTunnel(tunnelId),
            (nameof(ISshManager.StopTunnel), Guid tunnelId) => StopTunnel(tunnelId),

            ObservableBridge.SubscribeObservable<Change<TunnelConfig>> m => m.Apply(changes, observableDisposables),
            ObservableBridge.SubscribeObservable<TunnelState> m          => m.Apply(tunnelRunningStateChanges, observableDisposables),
            ObservableBridge.UnsubscribeObservable m                     => m.Apply(observableDisposables),

            _ => UnhandledEff(message)
        };

    Eff<Unit> StartTunnel(Guid tunnelId) =>
        from __ in unitEff
        let startTunnel =
            from tunnel in tunnels.GetEff(tunnelId)
            let config = tunnel.Config
            let createNewActor = from actor in Context.CreateActor<SshController>($"ssh-connection-{config.Id}", config)
                                 from _1 in tunnels.Set(tunnelId, tunnel with { Controller = Some(actor) })
                                 select actor
            from actor in tunnel.Controller.ToEff() | createNewActor
            let controller = new SshControllerWrapper(actor)
            from playState in controller.Start
            from _2 in eff(() => playState.Subscribe(isPlaying => tunnelRunningStateChanges.OnNext(new TunnelState(tunnelId, isPlaying))))
            select unit
        from _1 in Sender.Respond(startTunnel
                                | @catch(AppStandardErrors.NotFound, AppStandardErrors.NotFoundFromKey(tunnelId.ToString())))
        select unit;

    Eff<Unit> StopTunnel(Guid tunnelId) =>
        from __ in unitEff
        let stopTunnel =
            from tunnel in tunnels.GetEff(tunnelId)
            from actor in tunnel.Controller.ToEff()
                        | FailEff<IActorRef>(AppStandardErrors.Unexpected($"Controller for {tunnelId} has not been started"))
            let controller = new SshControllerWrapper(actor)
            from _1 in controller.DisposeEff()
            from _2 in tunnels.Set(tunnelId, tunnel with { Controller = None })
            select unit
        from _1 in Sender.Respond(stopTunnel
                                | @catch(AppStandardErrors.NotFound, AppStandardErrors.NotFoundFromKey(tunnelId.ToString())))
        select unit;

    // TODO: move this into SshController
    protected override void PostStop() {
        changes.OnCompleted();
        tunnelRunningStateChanges.OnCompleted();
        observableDisposables.Values.Iter(d => d.Dispose());
    }

    protected override Eff<Unit> PreStartEff { get; }
    public IStash Stash { get; set; } = default!;
}