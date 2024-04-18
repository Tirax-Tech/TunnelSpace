using System.Runtime.CompilerServices;

namespace Tirax.TunnelSpace.EffHelpers;

public static class DisposableEff
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<Unit> DisposeEff(this IDisposable disposable) =>
        eff(disposable.Dispose);
}