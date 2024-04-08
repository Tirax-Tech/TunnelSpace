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
}