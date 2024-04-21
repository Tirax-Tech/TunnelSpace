using System.Runtime.CompilerServices;
using Akka.Actor;

namespace Tirax.TunnelSpace.Services.Akka;

public abstract class UntypedUnitActor : UntypedActor
{
    protected abstract Unit HandleReceive(object message);

    protected virtual Unit OnPreStart()                               => unit;
    protected virtual Unit OnPostStop()                               => unit;
    protected virtual Unit OnPreRestart(Error reason, object message) => unit;
    protected virtual Unit OnPostRestart(Error reason)                => unit;

    protected sealed override void PreStart() => OnPreStart();
    protected sealed override void PostStop() => OnPostStop();

    protected sealed override void PreRestart(Exception reason, object message) => OnPreRestart(reason, message);
    protected sealed override void PostRestart(Exception reason)                => OnPostRestart(reason);

    protected sealed override void OnReceive(object message) => HandleReceive(message);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Unit BecomeStacked(Func<object,Unit> receive) {
        base.BecomeStacked(m => receive(m));
        return unit;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected new Unit UnbecomeStacked() {
        base.UnbecomeStacked();
        return unit;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected new Unit Unhandled(object message) {
        base.Unhandled(message);
        return unit;
    }
}