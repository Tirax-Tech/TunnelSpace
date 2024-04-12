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

public sealed class SshManager(IActorRef manager) : ISshManager
{
    public Aff<ISshController> CreateSshController(TunnelConfig config) =>
        manager.AskEff<ISshController>((nameof(CreateSshController), config));
}

public sealed class SshManagerActor : UntypedActor
{
    protected override void OnReceive(object message) =>
        (message switch
         {
             (nameof(ISshManager.CreateSshController), TunnelConfig config) =>
                 Sender.Respond(from process in SuccessEff(StartSshProcess(config))
                                from actor in Context.CreateActor<SshController>(() => new SshController(process),
                                                                                 $"ssh-connection-{config.Id}")
                                select new SshControllerWrapper(actor)),

             _ => eff(() => Unhandled(message))
         }
        ).RunUnit();

    static Eff<Process> StartSshProcess(TunnelConfig config) {
        var portParameters = IsPortUnspecified(config.Port)? Seq.empty<string>() : Seq("-p ", config.Port.ToString());
        var processParameters = portParameters.Concat(Seq("-fN", config.Host,
                                                          "-L", $"{config.LocalPort}:{config.RemoteHost}:{config.RemotePort}"));
        return Eff(() => Process.Start("ssh", processParameters));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsPortUnspecified(short port) => port == 0;
}