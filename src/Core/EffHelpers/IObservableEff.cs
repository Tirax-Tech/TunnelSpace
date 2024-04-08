using System.Runtime.CompilerServices;
using ReactiveUI;

namespace Tirax.TunnelSpace.EffHelpers;

public static class IObservableEff
{
    public static Eff<IDisposable> SubscribeEff<T>(this IObservable<T> observable, Func<T,Eff<Unit>> handler) =>
        Eff(() => observable.Subscribe(x => handler(x).RunUnit()));


    public static Eff<T> ChangeProperty<T>(this IReactiveObject sender, string caller, Eff<T> viewSetter) =>
        from _1 in Eff(() => {
            sender.RaisePropertyChanging(caller);
            return unit;
        })
        from result in viewSetter
        from _3 in Eff(() => {
            sender.RaisePropertyChanged(caller);
            return unit;
        })
        select result;
}