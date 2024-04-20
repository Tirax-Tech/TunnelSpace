using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Tirax.TunnelSpace.Helpers;

namespace Tirax.TunnelSpace.Services.Akka;

public static class ObservableBridge
{
    public sealed record SubscribeObservable<T>(Guid Id, IObserver<T> Observer)
    {
        public Unit Apply(Subject<T> subject, IDictionary<Guid, IDisposable> disposableTracker) {
            disposableTracker[Id] = subject.Subscribe(Observer);
            return unit;
        }
    }

    public sealed record UnsubscribeObservable(Guid Id)
    {
        public Unit Apply(IDictionary<Guid, IDisposable> disposableTracker) {
            if (disposableTracker.TakeOut(Id).IfSome(out var disposable))
                disposable.Dispose();
            return unit;
        }
    }

    public static IObservable<T> CreateObservable<T>(this IActorRef actor, IUniqueId uniqueId) =>
        Observable.Create<T>(observer => {
                                 var id = uniqueId.NewGuid();
                                 actor.Tell(new SubscribeObservable<T>(id, observer));
                                 return () => actor.Tell(new UnsubscribeObservable(id));
                             });
}