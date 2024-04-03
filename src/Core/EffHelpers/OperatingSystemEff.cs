namespace Tirax.TunnelSpace.EffHelpers;

public static class OperatingSystemEff
{
    public static readonly Eff<bool> IsWindows =
        Eff(OperatingSystem.IsWindows);
}