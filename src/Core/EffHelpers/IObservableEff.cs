using System;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace Tirax.TunnelSpace.EffHelpers;

public static class IObservableEff
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<IDisposable> SubscribeEff<T>(this IObservable<T> observable, IObserver<T> observer) =>
        Eff(() => observable.Subscribe(observer));

    public static Eff<IDisposable> SubscribeEff<T>(this IObservable<T> observable, Func<T,Eff<Unit>> handler) =>
        Eff(() => observable.Subscribe(x => handler(x).RunUnit()));

    public static Eff<T> ChangeProperties<T>(this IReactiveObject sender, Seq<string> caller, Eff<T> viewSetter) =>
        from _1 in eff(() => caller.Iter(sender.RaisePropertyChanging))
        from result in viewSetter
        from _2 in eff(() => caller.Iter(sender.RaisePropertyChanged))
        select result;

    public static Eff<T> ChangeProperty<T>(this IReactiveObject sender, string caller, Eff<T> viewSetter) =>
        from _1 in eff(() => sender.RaisePropertyChanging(caller))
        from result in viewSetter
        from _2 in eff(() => sender.RaisePropertyChanged(caller))
        select result;

    public static Eff<Unit> OnNextEff<T>(this IObserver<T> observer, T value) =>
        eff(() => observer.OnNext(value));
}