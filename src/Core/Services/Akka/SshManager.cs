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

public sealed class SshManager(IUniqueId uniqueId, IActorRef manager) : ISshManager
{
    public Aff<TunnelConfig[]> All { get; } = manager.AskEff<TunnelConfig[]>(nameof(All));

    public Aff<Unit> AddTunnel(TunnelConfig config) => manager.AskEff<Unit>((nameof(AddTunnel), config));
    public Aff<Unit> UpdateTunnel(TunnelConfig config) => manager.AskEff<Unit>((nameof(UpdateTunnel), config));
    public Aff<Unit> DeleteTunnel(Guid tunnelId) => manager.AskEff<Unit>((nameof(DeleteTunnel), tunnelId));

    public Aff<Unit> StartTunnel(Guid tunnelId)  => manager.AskEff<Unit>((nameof(StartTunnel), tunnelId));
    public Aff<Unit> StopTunnel(Guid tunnelId)   => manager.AskEff<Unit>((nameof(StopTunnel), tunnelId));

    public IObservable<Change<TunnelConfig>> Changes { get; } = manager.CreateObservable<Change<TunnelConfig>>(uniqueId);
    public IObservable<TunnelState> TunnelRunningStateChanges { get; } = manager.CreateObservable<TunnelState>(uniqueId);
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

            (nameof(ISshManager.AddTunnel), TunnelConfig config) => Sender.Respond(AddTunnel(config)),
            (nameof(ISshManager.UpdateTunnel), TunnelConfig config) => Sender.Respond(UpdateTunnel(config)),
            (nameof(ISshManager.DeleteTunnel), Guid tunnelId) => Sender.Respond(DeleteTunnel(tunnelId)),

            (nameof(ISshManager.StartTunnel), Guid tunnelId) => Sender.Respond(StartTunnel(tunnelId)),
            (nameof(ISshManager.StopTunnel), Guid tunnelId) => Sender.Respond(StopTunnel(tunnelId)),

            ObservableBridge.SubscribeObservable<Change<TunnelConfig>> m => m.Apply(changes, observableDisposables),
            ObservableBridge.SubscribeObservable<TunnelState> m          => m.Apply(tunnelRunningStateChanges, observableDisposables),
            ObservableBridge.UnsubscribeObservable m                     => m.Apply(observableDisposables),

            _ => UnhandledEff(message)
        };

    Aff<Unit> AddTunnel(TunnelConfig config) =>
        from _1 in storage.Add(config)
        from _2 in tunnels.Set(config.Id!.Value, new SshTunnelItem(config))
        from _3 in changes.OnNextEff(Change<TunnelConfig>.Added(config))
        select unit;

    Aff<Unit> UpdateTunnel(TunnelConfig config) =>
        from old in tunnels.GetEff(config.Id!.Value)
        from _1 in storage.Update(config)
        from _2 in StopController(old) | @catch(AppErrors.ControllerNotStarted, unit)
        from _3 in tunnels.Set(config.Id!.Value, new(config))
        from _4 in changes.OnNextEff(Change<TunnelConfig>.Mapped(old.Config, config))
        select unit;

    Aff<Unit> DeleteTunnel(Guid tunnelId) =>
        from tunnel in tunnels.TakeOut(tunnelId)
        from _1 in StopController(tunnel) | @catch(AppErrors.ControllerNotStarted, unit)
        from _2 in storage.Delete(tunnelId)
        from _3 in changes.OnNextEff(Change<TunnelConfig>.Removed(tunnel.Config))
        select unit;

    Aff<Unit> StartTunnel(Guid tunnelId) =>
        from tunnel in tunnels.GetEff(tunnelId, tunnelId.ToString())
        let config = tunnel.Config
        let createNewActor = from actor in Context.CreateActor<SshController>($"ssh-connection-{config.Id}", config)
                             from _1 in tunnels.Set(tunnelId, tunnel with { Controller = Some(actor) })
                             select actor
        from actor in tunnel.Controller.ToEff() | createNewActor
        let controller = new SshControllerWrapper(actor)
        from playState in controller.Start
        from _2 in eff(() => playState.Subscribe(isPlaying => tunnelRunningStateChanges.OnNext(new TunnelState(tunnelId, isPlaying))))
        select unit;

    Aff<Unit> StopTunnel(Guid tunnelId) =>
        from tunnel in tunnels.GetEff(tunnelId, tunnelId.ToString())
        from _ in StopController(tunnel)
        select unit;

    Eff<Unit> StopController(SshTunnelItem tunnel) =>
        from tunnelId in SuccessEff(tunnel.Config.Id!.Value)
        from actor in tunnel.Controller.ToEff() | FailEff<IActorRef>(AppErrors.ControllerNotStarted)
        let controller = new SshControllerWrapper(actor)
        from _1 in controller.DisposeEff()
        from _2 in tunnels.Set(tunnelId, tunnel with { Controller = None })
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