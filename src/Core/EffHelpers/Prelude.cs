using System.Runtime.CompilerServices;

namespace Tirax.TunnelSpace.EffHelpers;

public static class Prelude
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<Unit> eff(Action action) =>
        Eff(() => {
            action();
            return unit;
        });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<Unit> aff(Func<Task> action) =>
        Aff(async () => {
            await action();
            return unit;
        });
}