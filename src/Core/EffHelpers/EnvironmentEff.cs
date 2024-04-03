namespace Tirax.TunnelSpace.EffHelpers;

public static class EnvironmentEff
{
    public static Eff<string> GetFolderPath(Environment.SpecialFolder folder) =>
        Eff(() => Environment.GetFolderPath(folder));
}