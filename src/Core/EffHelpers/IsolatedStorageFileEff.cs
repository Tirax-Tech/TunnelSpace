using System.IO.IsolatedStorage;

namespace Tirax.TunnelSpace.EffHelpers;

public static class IsolatedStorageFileEff
{
    public static Eff<IsolatedStorageFileStream> OpenFileEff(this IsolatedStorageFile store, string path, FileMode mode) =>
        Eff(() => store.OpenFile(path, mode));

    public static Eff<Unit> CloseEff(this IsolatedStorageFileStream stream) =>
        eff(stream.Close);
}