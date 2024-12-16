using System.Reactive.Subjects;
using Akka.Actor;
using RZ.Foundation.Akka;
using RZ.Foundation.Types;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.Helpers;

namespace Tirax.TunnelSpace.Services.Akka;

public sealed record TunnelState(TunnelConfig Config, bool IsRunning);

public interface ISshManager
{
    Task<TunnelState[]> RetrieveState();

    Task AddTunnel(TunnelConfig config);
    Task UpdateTunnel(TunnelConfig config);
    Task DeleteTunnel(Guid tunnelId);

    Task StartTunnel(Guid tunnelId);
    Task StopTunnel(Guid tunnelId);

    IObservable<Change<TunnelConfig>> Changes { get; }
    IObservable<TunnelState> TunnelRunningStateChanges { get; }
}

public sealed class SshManager(IUniqueId uniqueId, IActorRef manager) : ISshManager
{
    public Task<TunnelState[]> RetrieveState()
        => manager.Ask<TunnelState[]>(nameof(RetrieveState));

    public Task AddTunnel(TunnelConfig config) => manager.Ask((nameof(AddTunnel), config));
    public Task UpdateTunnel(TunnelConfig config) => manager.Ask((nameof(UpdateTunnel), config));
    public Task DeleteTunnel(Guid tunnelId) => manager.Ask((nameof(DeleteTunnel), tunnelId));

    public Task StartTunnel(Guid tunnelId)  => manager.Ask((nameof(StartTunnel), tunnelId));
    public Task StopTunnel(Guid tunnelId)   => manager.Ask((nameof(StopTunnel), tunnelId));

    public IObservable<Change<TunnelConfig>> Changes { get; } = manager.CreateObservable<Change<TunnelConfig>>(uniqueId);
    public IObservable<TunnelState> TunnelRunningStateChanges { get; } = manager.CreateObservable<TunnelState>(uniqueId);
}

public sealed class SshManagerActor : UntypedActor, IWithUnboundedStash
{
    sealed record SshTunnelItem(TunnelConfig Config, IActorRef? Controller = null);

    readonly ITunnelConfigStorage storage;
    Dictionary<Guid, SshTunnelItem> tunnels = new();
    readonly Dictionary<Guid, IDisposable> observableDisposables = new();
    readonly Subject<Change<TunnelConfig>> changes = new();
    readonly Subject<TunnelState> tunnelRunningStateChanges = new();

    public SshManagerActor(ITunnelConfigStorage storage) {
        this.storage = storage;
        var loadedData = storage.All();
        Self.Tell(loadedData.ToArray());
    }

    protected override void PreStart() {
        BecomeStacked(m => {
            if (m is TunnelConfig[] data)
                InitData(data);
            else
                Stash.Stash();
        });

        void InitData(TunnelConfig[] data) {
            tunnels = (from config in data
                       select KeyValuePair.Create(config.Id!.Value, new SshTunnelItem(config))
                      ).ToDictionary();
            UnbecomeStacked();
            Stash.UnstashAll();
        }
    }

    protected override void OnReceive(object message) {
        switch (message){
            case nameof(ISshManager.RetrieveState):                       Sender.Tell(RetrieveState()); break;
            case (nameof(ISshManager.AddTunnel), TunnelConfig config):    Sender.Respond(AddTunnel(config)); break;
            case (nameof(ISshManager.UpdateTunnel), TunnelConfig config): Sender.Respond(UpdateTunnel(config)); break;
            case (nameof(ISshManager.DeleteTunnel), Guid tunnelId):       Sender.Respond(DeleteTunnel(tunnelId)); break;
            case (nameof(ISshManager.StartTunnel), Guid tunnelId):        Sender.Respond(StartTunnel(tunnelId)); break;

            case (nameof(ISshManager.StopTunnel), Guid tunnelId):
                StopTunnel(tunnelId);
                Sender.TellUnit();
                break;

            case ObservableBridge.SubscribeObservable<Change<TunnelConfig>> m: m.Apply(changes, observableDisposables); break;
            case ObservableBridge.SubscribeObservable<TunnelState> m:          m.Apply(tunnelRunningStateChanges, observableDisposables); break;
            case ObservableBridge.UnsubscribeObservable m:                     m.Apply(observableDisposables); break;

            default: Unhandled(message); break;
        }
    }

    TunnelState[] RetrieveState() =>
        tunnels.Values.Map(t => new TunnelState(t.Config, t.Controller is not null)).ToArray();

    async Task AddTunnel(TunnelConfig config) {
        await storage.Add(config);
        tunnels[config.Id!.Value] = new SshTunnelItem(config);
        changes.OnNext(Change<TunnelConfig>.Added(config));
    }

    async Task UpdateTunnel(TunnelConfig config) {
        var old = tunnels.Get(config.Id!.Value).Get();
        await storage.Update(config);
        StopController(old);
        tunnels[config.Id!.Value] = new(config);
        changes.OnNext(Change<TunnelConfig>.Mapped(old.Config, config));
    }

    async Task DeleteTunnel(Guid tunnelId) {
        var tunnel = tunnels.TakeOut(tunnelId).GetOrThrow(CatchKeyNotFound(tunnelId));
        StopController(tunnel);
        await storage.Delete(tunnelId);
        changes.OnNext(Change<TunnelConfig>.Removed(tunnel.Config));
    }

    async Task StartTunnel(Guid tunnelId) {
        var tunnel = tunnels.TakeOut(tunnelId).GetOrThrow(CatchKeyNotFound(tunnelId));
        var actor = tunnel.Controller ?? CreateController(tunnelId, tunnel);
        var controller = new SshControllerWrapper(actor);
        var playState = await controller.Start();
        // new observable is always created, no need for disposal.
        playState.Subscribe(isPlaying => tunnelRunningStateChanges.OnNext(new TunnelState(tunnel.Config, isPlaying)));
    }

    IActorRef CreateController(Guid tunnelId, SshTunnelItem tunnel) {
        var config = tunnel.Config;
        var actor = Context.CreateActor<SshController>($"ssh-connection-{config.Id}", config);
        tunnels[tunnelId] = tunnel with { Controller = actor };
        return actor;
    }

    void StopTunnel(Guid tunnelId) {
        var tunnel = tunnels.TakeOut(tunnelId).GetOrThrow(CatchKeyNotFound(tunnelId));
        StopController(tunnel);
    }

    static Func<Exception> CatchKeyNotFound(Guid tunnelId) => () =>
        new ErrorInfoException(StandardErrorCodes.NotFound, $"Tunnel with id {tunnelId} not found");

    void StopController(SshTunnelItem tunnel) {
        if (tunnel.Controller is null)
            throw new ErrorInfoException(AppErrors.ControllerNotStarted, null);
        var actor = tunnel.Controller;
        var controller = new SshControllerWrapper(actor);
        controller.Dispose();
        var tunnelId = tunnel.Config.Id!.Value;
        tunnels[tunnelId] = tunnel with { Controller = null };
    }

    protected override void PostStop() {
        changes.OnCompleted();
        tunnelRunningStateChanges.OnCompleted();
        observableDisposables.Values.Iter(d => d.Dispose());
    }

    public IStash Stash { get; set; } = null!;
}