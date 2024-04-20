using Akka.Actor;

namespace Tirax.TunnelSpace.Services.Akka;

public abstract class UntypedUnitActor : UntypedActor
{
    protected sealed override void OnReceive(object message) => HandleReceive(message);

    protected abstract Unit HandleReceive(object message);

    protected new Unit Unhandled(object message) {
        base.Unhandled(message);
        return unit;
    }
}