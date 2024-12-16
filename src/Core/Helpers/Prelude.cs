using System.Collections;
using System.Runtime.CompilerServices;
using RZ.Foundation.Types;

namespace RZ.Foundation;

public static class PreludeX
{
    public class ForwardReadOnlyCollection<T>(ICollection<T> collection) : IReadOnlyCollection<T>
    {
        public IEnumerator<T>   GetEnumerator() => collection.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => collection.GetEnumerator();
        public int Count => collection.Count;
    }

    public static IReadOnlyCollection<T> ToReadOnlyCollection<T>(this ICollection<T> collection)
        => new ForwardReadOnlyCollection<T>(collection);

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static Unit Ignore<T>(this T _) => unit;

    #region Try

    public static Exception? Try<S>(S state, Action<S> f)
    {
        try
        {
            f(state);
            return null;
        }
        catch (Exception e){
            return e;
        }
    }

    public static Exception? Try(Action f)
    {
        try{
            f();
            return null;
        }
        catch (Exception e){
            return e;
        }
    }

    public static async Task<Exception?> Try(Func<Task> f)
    {
        try
        {
            await f();
            return null;
        }
        catch (Exception e){
            return e;
        }
    }

    public static async Task<Exception?> Try<S>(S state, Func<S, Task> f)
    {
        try
        {
            await f(state);
            return null;
        }
        catch (Exception e){
            return e;
        }
    }

    public static void Catch<TState>(TState state, Action<TState> f, string errorCode, string? message) {
        try{
            f(state);
        }
        catch (Exception e){
            throw new ErrorInfoException(errorCode, message ?? errorCode, innerException: e);
        }
    }

    public static (Exception? Error, T Value) Try<S,T>(S state, Func<S,T> f)
    {
        try
        {
            return (null, f(state));
        }
        catch (Exception e)
        {
            return (e, default!);
        }
    }

    public static (Exception? Error, T Value) Try<T>(Func<T> f)
    {
        try
        {
            return (null, f());
        }
        catch (Exception e)
        {
            return (e, default!);
        }
    }

    public static async Task<(Exception? Error, T Value)> Try<T>(Func<Task<T>> f)
    {
        try
        {
            return (null, await f());
        }
        catch (Exception e)
        {
            return (e, default!);
        }
    }

    public static async Task<(Exception? Error, T Value)> Try<S,T>(S state, Func<S, Task<T>> f)
    {
        try
        {
            return (null, await f(state));
        }
        catch (Exception e)
        {
            return (e, default!);
        }
    }

    public static T Catch<TState, T>(TState state, Func<TState, T> f, string errorCode, string? message) {
        try{
            return f(state);
        }
        catch (Exception e){
            throw new ErrorInfoException(errorCode, message ?? errorCode, innerException: e);
        }
    }

    #endregion
}