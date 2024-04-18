using System.Diagnostics;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Tirax.TunnelSpace.Domain;
using Tirax.TunnelSpace.EffHelpers;
using Seq = LanguageExt.Seq;

namespace Tirax.TunnelSpace.Services.Akka;

public interface ISshController : IDisposable
{
    Aff<IObservable<bool>> Start { get; }
}

sealed class SshControllerWrapper(IActorRef actor) : ISshController
{
    public Aff<IObservable<bool>> Start { get; } =
        actor.AskEff<IObservable<bool>>(nameof(Start));

    public void Dispose() =>
        actor.Tell(nameof(Dispose));
}

public sealed class SshController(TunnelConfig config) : UntypedActor
{
    BehaviorSubject<bool> state = new(false);
    Option<Process> process;

    protected override void PostStop() {
        CloseCurrentProcess().RunUnit();
    }

    protected override void OnReceive(object message) =>
        (message switch
         {
             nameof(ISshController.Start) =>
                 Sender.Respond(from _1 in CloseCurrentProcess()
                                from started in StartSshProcess(config)
                                from _2 in OnStarted(started)
                                select state),

             nameof(IDisposable.Dispose) =>
                 from _1 in CloseCurrentProcess()
                 from _2 in eff(() => Self.Tell(PoisonPill.Instance))
                 select unit,

             "CheckProcess" => eff(() => process.Iter(p => p.Refresh())),

             _ => unitEff
         }
        ).RunUnit();

    static Eff<Process> StartSshProcess(TunnelConfig config) {
        var portParameters = IsPortUnspecified(config.Port)? Seq.empty<string>() : Seq("-p", config.Port.ToString());
        var processParameters = portParameters.Concat(Seq("-fN", config.Host,
                                                          "-L", $"{config.LocalPort}:{config.RemoteHost}:{config.RemotePort}"));
        return Eff(() => Process.Start("ssh", processParameters));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsPortUnspecified(short port) => port is 0 or 22;

    Eff<Unit> OnStarted(Process p) =>
        eff(() => {
                process = p;
                state.OnNext(true);
                p.EnableRaisingEvents = true;
                p.Exited += (_, _) => CloseCurrentProcess().RunUnit();
            });

    Eff<Unit> CloseCurrentProcess() =>
        eff(() => {
                if (process.IfSome(out var p)) {
                    state.OnNext(false);
                    state.OnCompleted();
                    state = new(false);
                    process = None;

                    using var _ = p;
                    if (!p.HasExited)
                        p.Kill();
                }
            });
}