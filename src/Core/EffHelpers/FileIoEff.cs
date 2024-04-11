namespace Tirax.TunnelSpace.EffHelpers;

public static class FileIoEff
{
    public static Eff<string> ReadAllText(string path) =>
        Eff(() => File.ReadAllText(path));
}