using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Akka.Actor;
using RZ.Foundation.Akka;
using RZ.Foundation.Functional;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.Helpers;

namespace Tirax.TunnelSpace.Services.Akka;

public sealed record TunnelState(TunnelConfig Config, bool IsRunning);

public interface ISshManager
{
    OutcomeAsync<Seq<TunnelState>> RetrieveState();

    OutcomeAsync<Unit> AddTunnel(TunnelConfig config);
    OutcomeAsync<Unit> UpdateTunnel(TunnelConfig config);
    OutcomeAsync<Unit> DeleteTunnel(Guid tunnelId);

    OutcomeAsync<Unit> StartTunnel(Guid tunnelId);
    OutcomeAsync<Unit> StopTunnel(Guid tunnelId);

    IObservable<Change<TunnelConfig>> Changes { get; }
    IObservable<TunnelState> TunnelRunningStateChanges { get; }
}

public sealed class SshManager(IUniqueId uniqueId, IActorRef manager) : ISshManager
{
    public OutcomeAsync<Seq<TunnelState>> RetrieveState() =>
        from configs in manager.SafeAsk<TunnelState[]>(nameof(RetrieveState))
        select configs.ToSeq();

    public OutcomeAsync<Unit> AddTunnel(TunnelConfig config) => manager.SafeAsk<Unit>((nameof(AddTunnel), config));
    public OutcomeAsync<Unit> UpdateTunnel(TunnelConfig config) => manager.SafeAsk<Unit>((nameof(UpdateTunnel), config));
    public OutcomeAsync<Unit> DeleteTunnel(Guid tunnelId) => manager.SafeAsk<Unit>((nameof(DeleteTunnel), tunnelId));

    public OutcomeAsync<Unit> StartTunnel(Guid tunnelId)  => manager.SafeAsk<Unit>((nameof(StartTunnel), tunnelId));
    public OutcomeAsync<Unit> StopTunnel(Guid tunnelId)   => manager.SafeAsk<Unit>((nameof(StopTunnel), tunnelId));

    public IObservable<Change<TunnelConfig>> Changes { get; } = manager.CreateObservable<Change<TunnelConfig>>(uniqueId);
    public IObservable<TunnelState> TunnelRunningStateChanges { get; } = manager.CreateObservable<TunnelState>(uniqueId);
}

public sealed class SshManagerActor : UntypedUnitActor, IWithUnboundedStash
{
    sealed record SshTunnelItem(TunnelConfig Config, Option<IActorRef> Controller = default);

    readonly ITunnelConfigStorage storage;
    Dictionary<Guid, SshTunnelItem> tunnels = new();
    readonly Dictionary<Guid, IDisposable> observableDisposables = new();
    readonly Subject<Change<TunnelConfig>> changes = new();
    readonly Subject<TunnelState> tunnelRunningStateChanges = new();

    public SshManagerActor(ITunnelConfigStorage storage) {
        this.storage = storage;
        var loadedData = from data in storage.All()
                         select data.ToArray();
        loadedData.AsTask().PipeTo(Self);
    }

    protected override Unit OnPreStart() {
        return BecomeStacked(
            m => m switch
                 {
                     Outcome<TunnelConfig[]> data => (from allData in data
                                                      select InitData(allData)
                                                     ).Unwrap(),
                     _ => ToUnit(Stash.Stash)
                 });

        Unit InitData(TunnelConfig[] data) {
            tunnels = (from config in data
                       select KeyValuePair.Create(config.Id!.Value, new SshTunnelItem(config))
                      ).ToDictionary();
            UnbecomeStacked();
            Stash.UnstashAll();
            return unit;
        }
    }

    protected override Unit HandleReceive(object message) =>
        message switch
        {
            nameof(ISshManager.RetrieveState) => Sender.TellUnit(RetrieveState()),

            (nameof(ISshManager.AddTunnel), TunnelConfig config)    => Sender.Respond(AddTunnel(config)),
            (nameof(ISshManager.UpdateTunnel), TunnelConfig config) => Sender.Respond(UpdateTunnel(config)),
            (nameof(ISshManager.DeleteTunnel), Guid tunnelId)       => Sender.Respond(DeleteTunnel(tunnelId)),

            (nameof(ISshManager.StartTunnel), Guid tunnelId) => Sender.Respond(StartTunnel(tunnelId)),
            (nameof(ISshManager.StopTunnel), Guid tunnelId)  => Sender.TellUnit(StopTunnel(tunnelId)),

            ObservableBridge.SubscribeObservable<Change<TunnelConfig>> m => m.Apply(changes, observableDisposables),
            ObservableBridge.SubscribeObservable<TunnelState> m          => m.Apply(tunnelRunningStateChanges, observableDisposables),
            ObservableBridge.UnsubscribeObservable m                     => m.Apply(observableDisposables),

            _ => Unhandled(message)
        };

    Outcome<TunnelState[]> RetrieveState() =>
        tunnels.Values.Map(t => new TunnelState(t.Config, t.Controller.IsSome)).ToArray();

    OutcomeAsync<Unit> AddTunnel(TunnelConfig config) =>
        storage.Add(config).Map(_ => unit)
      | @do<Unit>(_ => {
                      tunnels[config.Id!.Value] = new SshTunnelItem(config);
                      changes.OnNext(Change<TunnelConfig>.Added(config));
                  });

    OutcomeAsync<Unit> UpdateTunnel(TunnelConfig config) =>
        from old in tunnels.Get(config.Id!.Value).ToOutcome()
        from _1 in storage.Update(config)
        from _2 in StopController(old)
                 | ifError(AppErrors.ControllerNotStarted, unit)
                 | @do<Unit>(_ => {
                                 tunnels[config.Id!.Value] = new(config);
                                 changes.OnNext(Change<TunnelConfig>.Mapped(old.Config, config));
                             })
        select unit;

    OutcomeAsync<Unit> DeleteTunnel(Guid tunnelId) =>
        from tunnel in tunnels.TakeOut(tunnelId).ToOutcome() | CatchKeyNotFound(tunnelId)
        from _1 in StopController(tunnel) | ifError(AppErrors.ControllerNotStarted, unit)
        from _2 in storage.Delete(tunnelId)
                 | @do<TunnelConfig>(_ => changes.OnNext(Change<TunnelConfig>.Removed(tunnel.Config)))
        select unit;

    OutcomeAsync<Unit> StartTunnel(Guid tunnelId) {
        return from tunnel in GetTunnel(tunnelId)
               from actor in tunnel.Controller.ToOutcome() | ifError(StandardErrors.NotFound, CreateController(tunnel))
               let controller = new SshControllerWrapper(actor)
               from playState in controller.Start() | @do(SetIsPlaying(tunnel.Config))
               select unit;

        Func<Error, IActorRef> CreateController(SshTunnelItem tunnel) =>
            _ => {
                var config = tunnel.Config;
                var actor = Context.CreateActor<SshController>($"ssh-connection-{config.Id}", config);
                tunnels[tunnelId] = tunnel with { Controller = Some(actor) };
                return actor;
            };

        Action<IObservable<bool>> SetIsPlaying(TunnelConfig config) =>
            playState => playState.Subscribe(isPlaying => tunnelRunningStateChanges.OnNext(new TunnelState(config, isPlaying)));
    }

    Outcome<Unit> StopTunnel(Guid tunnelId) =>
        from tunnel in GetTunnel(tunnelId)
        from _ in StopController(tunnel)
        select unit;

    Outcome<SshTunnelItem> GetTunnel(Guid tunnelId) =>
        tunnels.Get(tunnelId).ToOutcome() | CatchKeyNotFound(tunnelId);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static CatchError CatchKeyNotFound(Guid tunnelId) =>
        @ifError(StandardErrors.NotFound, _ => StandardErrors.NotFoundFromKey(tunnelId.ToString()));

    Outcome<Unit> StopController(SshTunnelItem tunnel) =>
        from _ in tunnel.Controller.ToOutcome()
                | FailedOutcome<IActorRef>(AppErrors.ControllerNotStarted)
                | @do<IActorRef>(actor => {
                                     var controller = new SshControllerWrapper(actor);
                                     controller.Dispose();
                                     var tunnelId = tunnel.Config.Id!.Value;
                                     tunnels[tunnelId] = tunnel with { Controller = None };
                                     return unit;
                                 })
        select unit;

    protected override Unit OnPostStop() {
        changes.OnCompleted();
        tunnelRunningStateChanges.OnCompleted();
        observableDisposables.Values.Iter(d => d.Dispose());
        return unit;
    }

    public IStash Stash { get; set; } = default!;
}