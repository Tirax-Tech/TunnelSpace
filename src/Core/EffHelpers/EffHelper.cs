using System.Runtime.CompilerServices;

namespace Tirax.TunnelSpace.EffHelpers;

public static class EffHelper
{
    public static void RunIgnore<T>(this Aff<T> aff) =>
        Task.Run(async () => (await aff.Run()).ThrowIfFail());

    public static Eff<Unit> ToBackground<T>(this Aff<T> aff) =>
        Eff(() => {
            Task.Run(async () => (await aff.Run()).ThrowIfFail());
            return unit;
        });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<Unit> Ignore<T>(this Eff<T> eff) => eff.Map(_ => unit);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<Unit> Ignore<T>(this Aff<T> eff) => eff.Map(_ => unit);
}