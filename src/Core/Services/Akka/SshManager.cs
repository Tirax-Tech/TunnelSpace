using System.Diagnostics;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.EffHelpers;
using Seq = LanguageExt.Seq;

namespace Tirax.TunnelSpace.Services.Akka;

public interface ISshManager
{
    Aff<ISshController> CreateSshController(TunnelConfig config);
}

public sealed class SshManager(ActorSystem system) : ISshManager
{
    readonly Eff<IActorRef> sshManager = system.CreateActor(() => new SshManagerActor(StartSshProcess), "ssh-manager").Memo();

    public Aff<ISshController> CreateSshController(TunnelConfig config) =>
        from manager in sshManager
        from actor in manager.AskEff<ISshController>((nameof(CreateSshController), config))
        select actor;

    static Eff<Process> StartSshProcess(TunnelConfig config) {
        var portParameters = IsPortUnspecified(config.Port)? Seq.empty<string>() : Seq("-p ", config.Port.ToString());
        var processParameters = portParameters.Concat(Seq("-fN", config.Host,
                                                          "-L", $"{config.LocalPort}:{config.RemoteHost}:{config.RemotePort}"));
        return Eff(() => Process.Start("ssh", processParameters));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsPortUnspecified(short port) => port == 0;
}

sealed class SshManagerActor(Func<TunnelConfig, Eff<Process>> startSsh) : UntypedActor
{
    protected override void OnReceive(object message) {
        var run =
            message switch
            {
                (nameof(ISshManager.CreateSshController), TunnelConfig config) =>
                    Sender.Respond(from process in SuccessEff(startSsh(config))
                                   from actor in Context.CreateActor<SshController>(() => new SshController(process),
                                                                                         $"ssh-connection-{config.Id}")
                                   select new SshControllerWrapper(actor)),

                _ => eff(() => Unhandled(message))
            };
        run.RunUnit();
    }
}