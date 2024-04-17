using Akka.Actor;

namespace Tirax.TunnelSpace.Services.Akka;

public abstract class UntypedActorEff : UntypedActor
{
    protected abstract Eff<Unit> OnReceiveEff(object message);
    protected virtual Eff<Unit> PreStartEff => unitEff;

    protected sealed override void OnReceive(object message) =>
        OnReceiveEff(message).RunUnit();

    protected sealed override void PreStart() {
        PreStartEff.RunUnit();
    }

    protected Eff<Unit> BecomeStacked(Func<object, Eff<Unit>> handler) =>
        eff(() => Become(message => handler(message).RunUnit()));

    protected new Eff<Unit> UnbecomeStacked { get; }

    protected UntypedActorEff() {
        UnbecomeStacked = eff(base.UnbecomeStacked);
    }

    protected Eff<Unit> UnhandledEff(object message) =>
        eff(() => Unhandled(message));
}