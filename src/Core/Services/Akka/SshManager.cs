using System.Diagnostics;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.EffHelpers;
using Seq = LanguageExt.Seq;

namespace Tirax.TunnelSpace.Services.Akka;

public sealed record TunnelState(Guid TunnelId, bool IsRunning);

public interface ISshManager
{
    Aff<TunnelConfig[]> All { get; }

    Aff<Unit> AddTunnel(TunnelConfig config);
    Aff<Unit> UpdateTunnel(TunnelConfig config);
    Aff<Unit> DeleteTunnel(Guid tunnelId);

    Aff<Unit> StartTunnel(Guid tunnelId);

    IObservable<Change<TunnelConfig>> Changes { get; }
    IObservable<TunnelState> TunnelRunningStateChanges { get; }
}

public sealed class SshManager : ISshManager
{
    public SshManager(IActorRef manager) {
        this.manager = manager;

        All = manager.AskEff<TunnelConfig[]>(nameof(All));
    }

    public Aff<TunnelConfig[]> All { get; }
    public Aff<Unit> AddTunnel(TunnelConfig config) {
        throw new NotImplementedException();
    }

    public Aff<Unit> UpdateTunnel(TunnelConfig config) {
        throw new NotImplementedException();
    }

    public Aff<ISshController> CreateSshController(TunnelConfig config) =>
        from actor in manager.AskEff<IActorRef>((nameof(CreateSshController), config))
        select (ISshController) new SshControllerWrapper(actor);

    public Aff<Unit> DeleteTunnel(Guid tunnelId) =>
        manager.AskEff<Unit>((nameof(DeleteTunnel), tunnelId));

    public Aff<Unit> StartTunnel(Guid tunnelId) {
        return manager.AskEff<Unit>((nameof(StartTunnel), tunnelId));
    }

    public IObservable<Change<TunnelConfig>> Changes { get; }
    public IObservable<TunnelState> TunnelRunningStateChanges { get; }

    readonly IActorRef manager;
}

public sealed class SshManagerActor : UntypedActorEff
{
    readonly ITunnelConfigStorage storage;

    sealed record SshTunnelItem(TunnelConfig Config, Option<IActorRef> Controller = default);
    Dictionary<Guid, SshTunnelItem> tunnels = new();

    // TODO: move this into SshController
    static Eff<Process> StartSshProcess(TunnelConfig config) {
        var portParameters = IsPortUnspecified(config.Port)? Seq.empty<string>() : Seq("-p", config.Port.ToString());
        var processParameters = portParameters.Concat(Seq("-fN", config.Host,
                                                          "-L", $"{config.LocalPort}:{config.RemoteHost}:{config.RemotePort}"));
        return Eff(() => Process.Start("ssh", processParameters));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsPortUnspecified(short port) => port is 0 or 22;

    protected override Eff<Unit> PreStartEff { get; }

    protected override Eff<Unit> OnReceiveEff(object message) =>
        message switch
        {
            (nameof(ISshManager.StartTunnel), Guid tunnelId) =>
                Sender.Respond(from config in Eff(() => tunnels[tunnelId].Config)
                               let process = StartSshProcess(config)
                               from actor in Context.CreateActor<SshController>($"ssh-connection-{config.Id}", process)
                               select actor),

            nameof(ISshManager.All) =>
                Sender.Respond(SuccessEff(tunnels.Values.Map(t => t.Config).ToArray())),

            (nameof(ISshManager.DeleteTunnel), Guid tunnelId) =>
                Sender.Respond(from _ in storage.Delete(tunnelId)
                               select unit),

            _ => eff(() => Unhandled(message))
        };

    public SshManagerActor(ITunnelConfigStorage storage) {
        this.storage = storage;
        PreStartEff = from _1 in BecomeStacked(m => m switch
                                                    {
                                                        Fin<TunnelConfig[]> data => from _1 in SetTunnels(data.ThrowIfFail())
                                                                                    from _2 in UnbecomeStacked
                                                                                    select unit,
                                                        _ => UnhandledEff(m)
                                                    })
                      select unit;
        Self.PipeFrom(from data in storage.All select data.ToArray());
        return;

        Eff<Unit> SetTunnels(TunnelConfig[] configs) =>
            eff(() => tunnels = (from config in configs
                                 select KeyValuePair.Create(config.Id!.Value, new SshTunnelItem(config))
                                ).ToDictionary());
    }
}