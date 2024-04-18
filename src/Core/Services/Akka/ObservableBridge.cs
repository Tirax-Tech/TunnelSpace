using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Tirax.TunnelSpace.EffHelpers;

namespace Tirax.TunnelSpace.Services.Akka;

public static class ObservableBridge
{
    public sealed record SubscribeObservable<T>(Guid Id, IObserver<T> Observer)
    {
        public Eff<Unit> Apply(Subject<T> subject, IDictionary<Guid, IDisposable> disposableTracker) =>
            from disposable in subject.SubscribeEff(Observer)
            from _ in disposableTracker.Set(Id, disposable)
            select unit;
    }

    public sealed record UnsubscribeObservable(Guid Id)
    {
        public Eff<Unit> Apply(IDictionary<Guid, IDisposable> disposableTracker) =>
            from ___ in unitEff
            let run = from disposable in disposableTracker.TakeOut(Id)
                      from _1 in disposable.DisposeEff()
                      select unit
            from _ in run | @catch(AppStandardErrors.NotFound, unit)
            select unit;
    }

    public static IObservable<T> CreateObservable<T>(this IActorRef actor, IUniqueId uniqueId) =>
        Observable.Create<T>(observer => (from id in uniqueId.NewGuid
                                          from _1 in actor.TellEff(new SubscribeObservable<T>(id, observer))

                                          let disposable = actor.TellEff(new UnsubscribeObservable(id))
                                          select new Action(() => disposable.RunUnit())
                                         ).Run().ThrowIfFail());
}