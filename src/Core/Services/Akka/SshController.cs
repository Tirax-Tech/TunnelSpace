using System.Diagnostics;
using System.Reactive.Subjects;
using Akka.Actor;
using Tirax.TunnelSpace.EffHelpers;

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

public sealed class SshController(Eff<Process> startSsh) : UntypedActor
{
    Subject<bool> state = new();
    Option<Process> process;

    protected override void OnReceive(object message) =>
        (message switch
         {
             nameof(ISshController.Start) =>
                 Sender.Respond(from _1 in CloseCurrentProcess()
                                from started in startSsh
                                from _2 in OnStarted(started)
                                select state),

             nameof(IDisposable.Dispose) =>
                 from _1 in CloseCurrentProcess()
                 from _2 in eff(() => Self.Tell(PoisonPill.Instance))
                 select unit,

             _ => unitEff
         }
        ).RunUnit();

    Eff<Unit> OnStarted(Process p) =>
        eff(() => {
                process = p;
                state.OnNext(true);
            });

    Eff<Unit> CloseCurrentProcess() =>
        eff(() => {
                if (process.IfSome(out var p)) {
                    state.OnNext(false);
                    state.OnCompleted();
                    state = new();
                    process = None;

                    using var _ = p;
                    if (!p.HasExited)
                        p.Kill();
                }
            });
}