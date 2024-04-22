using System.Runtime.CompilerServices;

namespace RZ.Foundation;

public static class PreludeX
{

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static Unit Ignore<T>(this T _) => unit;
}