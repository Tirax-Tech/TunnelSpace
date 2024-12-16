using System.Reactive.Linq;
using ReactiveUI;

namespace Tirax.TunnelSpace.Helpers;

public static class ObservableExtensions
{
    public static IDisposable SubscribeAsync<T>(this IObservable<T> observable, Func<T, Task> handler) =>
        observable.Select(x => Observable.FromAsync(async () => await handler(x)))
                  .Concat()
                  .Subscribe();

    public static T ChangeProperty<T>(this IReactiveObject sender, string caller, Func<T> viewSetter) {
        sender.RaisePropertyChanging(caller);
        var result = viewSetter();
        sender.RaisePropertyChanged(caller);
        return result;
    }
}