// ReSharper disable CheckNamespace
namespace RZ.Foundation;

public static class OutcomeExtensions
{
    public static OutcomeAsync<R> use<S,R>(this S source, Func<S,OutcomeAsync<R>> f) where S: IDisposable =>
        f(source) | @do<R>(_ => source.Dispose()) | @failDo(_ => source.Dispose());
}