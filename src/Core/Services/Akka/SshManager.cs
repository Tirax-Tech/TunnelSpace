using System.Reactive.Subjects;
using Akka.Actor;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.EffHelpers;

namespace Tirax.TunnelSpace.Services.Akka;

public sealed record TunnelState(TunnelConfig Config, bool IsRunning);

public interface ISshManager
{
    EitherAsync<Error, Seq<TunnelState>> RetrieveState();

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
    public EitherAsync<Error, Seq<TunnelState>> RetrieveState() =>
        from configs in manager.SafeAsk<TunnelState[]>(nameof(RetrieveState))
        select configs.ToSeq();

    public Aff<Unit> AddTunnel(TunnelConfig config) => manager.AskEff<Unit>((nameof(AddTunnel), config));
    public Aff<Unit> UpdateTunnel(TunnelConfig config) => manager.AskEff<Unit>((nameof(UpdateTunnel), config));
    public Aff<Unit> DeleteTunnel(Guid tunnelId) => manager.AskEff<Unit>((nameof(DeleteTunnel), tunnelId));

    public Aff<Unit> StartTunnel(Guid tunnelId)  => manager.AskEff<Unit>((nameof(StartTunnel), tunnelId));
    public Aff<Unit> StopTunnel(Guid tunnelId)   => manager.AskEff<Unit>((nameof(StopTunnel), tunnelId));

    public IObservable<Change<TunnelConfig>> Changes { get; } = manager.CreateObservable<Change<TunnelConfig>>(uniqueId);
    public IObservable<TunnelState> TunnelRunningStateChanges { get; } = manager.CreateObservable<TunnelState>(uniqueId);
}

public sealed class SshManagerActor : UntypedActor, IWithUnboundedStash
{
    sealed record SshTunnelItem(TunnelConfig Config, Option<IActorRef> Controller = default);

    readonly ITunnelConfigStorage storage;
    Dictionary<Guid, SshTunnelItem> tunnels = new();
    readonly Dictionary<Guid, IDisposable> observableDisposables = new();
    readonly Subject<Change<TunnelConfig>> changes = new();
    readonly Subject<TunnelState> tunnelRunningStateChanges = new();

    public SshManagerActor(ITunnelConfigStorage storage) {
        this.storage = storage;
        Self.PipeFrom(from data in storage.All select data.ToArray());
    }

    protected override void PreStart() {
        BecomeStacked(m => {
                          switch (m) {
                              case Fin<TunnelConfig[]> data: {
                                  tunnels = (from config in data.ThrowIfFail()
                                             select KeyValuePair.Create(config.Id!.Value, new SshTunnelItem(config))
                                            ).ToDictionary();
                                  UnbecomeStacked();
                                  Stash.UnstashAll();
                                  break;
                              }
                              default:
                                  ToUnit(Stash.Stash);
                                  break;
                          }
                      });
    }

    protected override void OnReceive(object message) =>
        Void(message switch
               {
                   nameof(ISshManager.RetrieveState) => Sender.TellEx(RetrieveState()),

                   (nameof(ISshManager.AddTunnel), TunnelConfig config)    => Sender.Respond(AddTunnel(config)).RunUnit(),
                   (nameof(ISshManager.UpdateTunnel), TunnelConfig config) => Sender.Respond(UpdateTunnel(config)).RunUnit(),
                   (nameof(ISshManager.DeleteTunnel), Guid tunnelId)       => Sender.Respond(DeleteTunnel(tunnelId)).RunUnit(),

                   (nameof(ISshManager.StartTunnel), Guid tunnelId) => Sender.Respond(StartTunnel(tunnelId)).RunUnit(),
                   (nameof(ISshManager.StopTunnel), Guid tunnelId)  => Sender.Respond(StopTunnel(tunnelId)).RunUnit(),

                   ObservableBridge.SubscribeObservable<Change<TunnelConfig>> m => m.Apply(changes, observableDisposables).RunUnit(),
                   ObservableBridge.SubscribeObservable<TunnelState> m          => m.Apply(tunnelRunningStateChanges, observableDisposables).RunUnit(),
                   ObservableBridge.UnsubscribeObservable m                     => m.Apply(observableDisposables).RunUnit(),

                   _ => UnhandledMessage(message)
               });

    Unit UnhandledMessage(object message) {
        Unhandled(message);
        return unit;
    }

    Either<Error, TunnelState[]> RetrieveState() =>
        tunnels.Values.Map(t => new TunnelState(t.Config, t.Controller.IsSome)).ToArray();

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
        from _2 in eff(() => playState.Subscribe(isPlaying => tunnelRunningStateChanges.OnNext(new TunnelState(config, isPlaying))))
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

    protected override void PostStop() {
        changes.OnCompleted();
        tunnelRunningStateChanges.OnCompleted();
        observableDisposables.Values.Iter(d => d.Dispose());
    }

    public IStash Stash { get; set; } = default!;
}