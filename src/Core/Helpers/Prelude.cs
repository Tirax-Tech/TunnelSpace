using System.Runtime.CompilerServices;

namespace RZ.Foundation;

public static class PreludeX
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<Unit> eff(Action action) => Eff(() => { action(); return unit; });

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Aff<Unit> aff(Task action) => Aff(async () => { await action; return unit; });

    public static OutcomeAsync<T> TryCatch<T>(Func<Task<Outcome<T>>> handler) =>
        from v in TryAsync(handler).ToOutcome()
        from result in v.ToAsync()
        select result;

    public static EitherAsync<Error, T> TryCatch<T>(Func<Task<Either<Error, T>>> handler) =>
        from v in TryAsync(handler).ToEither()
        from result in v.ToAsync()
        select result;

    public static EitherAsync<Error, T> TryCatch<T>(Func<ValueTask<Either<Error, T>>> handler) =>
        from v in TryAsync(async () => await handler()).ToEither()
        from result in v.ToAsync()
        select result;

    public static OutcomeAsync<T> TryCatch<T>(Func<Task<T>> handler) =>
        TryAsync(handler).ToOutcome();

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

    public static Outcome<T> TryCatch<T>(Func<T> handler) =>
        Try(handler).ToEither(Error.New);

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static Unit ___<T>(T v) => unit;
}