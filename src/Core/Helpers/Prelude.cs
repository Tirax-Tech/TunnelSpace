using System.Runtime.CompilerServices;

namespace RZ.Foundation;

public static class PreludeX
{
    public static EitherAsync<Error, T> TryCatch<T>(Func<Task<Either<Error, T>>> handler) =>
        from v in TryAsync(handler).ToEither()
        from result in v.ToAsync()
        select result;

    public static EitherAsync<Error, T> TryCatch<T>(Func<ValueTask<Either<Error, T>>> handler) =>
        from v in TryAsync(async () => await handler()).ToEither()
        from result in v.ToAsync()
        select result;

    public static EitherAsync<Error, T> TryCatch<T>(Func<Task<T>> handler) =>
        TryAsync(handler).ToEither();

    public static EitherAsync<Error, T> TryCatch<T>(Func<ValueTask<T>> handler) =>
        TryAsync(async () => await handler()).ToEither();

    public static EitherAsync<Error, Unit> TryCatch(Func<Task> handler) =>
        TryAsync(async () => {
            await handler();
            return Unit.Default;
        }).ToEither();

    public static EitherAsync<Error, Unit> TryCatch(Func<ValueTask> handler) =>
        TryAsync(async () => {
            await handler();
            return Unit.Default;
        }).ToEither();

    public static Either<Error, T> TryCatch<T>(Func<T> handler) =>
        Try(handler).ToEither(Error.New);

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static void Void<T>(T v) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Unit ToUnit(Action action) {
        action();
        return unit;
    }
}