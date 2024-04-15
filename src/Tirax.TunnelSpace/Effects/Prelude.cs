using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Avalonia.Threading;

namespace Tirax.TunnelSpace.Effects;

public static class Prelude
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<A> UiEff<A>(Func<A> f) =>
        Aff(async () => Dispatcher.UIThread.CheckAccess()
                            ? f()
                            : await Dispatcher.UIThread.InvokeAsync(f));

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<Unit> UiEff(Action f) =>
        Aff(async () => {
                if (Dispatcher.UIThread.CheckAccess())
                    f();
                else
                    await Dispatcher.UIThread.InvokeAsync(f);
                return unit;
            });
}