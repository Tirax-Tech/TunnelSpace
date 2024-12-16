using System.Diagnostics;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Tirax.TunnelSpace.Domain;
using Seq = LanguageExt.Seq;

namespace Tirax.TunnelSpace.Services.Akka;

public interface ISshController : IDisposable
{
    Task<IObservable<bool>> Start();
}

sealed class SshControllerWrapper(IActorRef actor) : ISshController
{
    public Task<IObservable<bool>> Start()
        => actor.Ask<IObservable<bool>>(nameof(Start));

    public void Dispose()
        => actor.Tell(nameof(Dispose));
}

public sealed class SshController(TunnelConfig config) : UntypedActor, IDisposable
{
    BehaviorSubject<bool> state = new(false);
    Process? process;

    public void Dispose() {
        CloseCurrentProcess();
        Self.Tell(PoisonPill.Instance);
    }

    protected override void PostStop() {
        CloseCurrentProcess();
    }

    protected override void OnReceive(object message) {
        switch(message) {
            case nameof(ISshController.Start): Sender.Tell(Start()); break;
            case nameof(IDisposable.Dispose):  Dispose(); break;

            case "CheckProcess": process?.Refresh(); break;

            default: Unhandled(message); break;
        }
    }

    IObservable<bool> Start() {
        CloseCurrentProcess();
        var p = StartSshProcess(config);
        p.EnableRaisingEvents = true;
        p.Exited += (_, _) => CloseCurrentProcess();
        process = p;
        state.OnNext(true);
        return state;
    }

    static Process StartSshProcess(TunnelConfig config) {
        var portParameters = IsPortUnspecified(config.Port) ? Seq.empty<string>() : Seq("-p", config.Port.ToString());
        var processParameters = portParameters.Concat(Seq("-fN", config.Host,
                                                          "-L", $"{config.LocalPort}:{config.RemoteHost}:{config.RemotePort}"));
        return Process.Start("ssh", processParameters);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool IsPortUnspecified(short port) => port is 0 or 22;

    void CloseCurrentProcess() {
        if (process is null) return;

        state.OnNext(false);
        state.OnCompleted();
        state = new(false);
        var p = process;
        process = null;

        using var _ = p;
        if (!p.HasExited)
            p.Kill();
    }
}