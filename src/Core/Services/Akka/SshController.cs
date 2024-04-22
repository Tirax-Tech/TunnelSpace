using System.Diagnostics;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Akka.Actor;
using RZ.Foundation.Akka;
using Tirax.TunnelSpace.Domain;
using Seq = LanguageExt.Seq;

namespace Tirax.TunnelSpace.Services.Akka;

public interface ISshController : IDisposable
{
    Aff<IObservable<bool>> Start();
}

sealed class SshControllerWrapper(IActorRef actor) : ISshController
{
    public Aff<IObservable<bool>> Start() =>
        actor.SafeAsk<IObservable<bool>>(nameof(Start));

    public void Dispose() =>
        actor.Tell(nameof(Dispose));
}

public sealed class SshController(TunnelConfig config) : UntypedUnitActor
{
    BehaviorSubject<bool> state = new(false);
    Option<Process> process;

    protected override Unit OnPostStop() =>
        CloseCurrentProcess();

    protected override Unit HandleReceive(object message) =>
        message switch
        {
            nameof(ISshController.Start) => Sender.TellUnit(Start()),
            nameof(IDisposable.Dispose)  => Dispose(),

            "CheckProcess" => process.Iter(p => p.Refresh()),

            _ => Unhandled(message)
        };

    Outcome<IObservable<bool>> Start() {
        CloseCurrentProcess();
        process = StartSshProcess(config);
        process.Iter(p => {
                         p.EnableRaisingEvents = true;
                         p.Exited += (_, _) => CloseCurrentProcess();
                     });
        state.OnNext(process.IsSome);
        return state;
    }

    Unit Dispose() {
        CloseCurrentProcess();
        Self.Tell(PoisonPill.Instance);
        return unit;
    }

    static Process StartSshProcess(TunnelConfig config) {
        var portParameters = IsPortUnspecified(config.Port) ? Seq.empty<string>() : Seq("-p", config.Port.ToString());
        var processParameters = portParameters.Concat(Seq("-fN", config.Host,
                                                          "-L", $"{config.LocalPort}:{config.RemoteHost}:{config.RemotePort}"));
        return Process.Start("ssh", processParameters);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsPortUnspecified(short port) => port is 0 or 22;

    Unit CloseCurrentProcess() {
        if (process.IfNone(out var p)) return unit;

        state.OnNext(false);
        state.OnCompleted();
        state = new(false);
        process = None;

        using var _ = p;
        if (!p.HasExited)
            p.Kill();
        return unit;
    }
}