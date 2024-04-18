using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.DependencyInjection;

namespace RZ.Foundation;

public static class ActorExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Props DependencyProps<T>(this ActorSystem sys, params object[] parameters) where T : ActorBase =>
        DependencyResolver.For(sys).Props<T>(parameters);

    // public static IActorRef CreateActor<T>(this ActorSystem sys, string name, params object[] parameters) where T : ActorBase =>
    //     sys.ActorOf(sys.DependencyProps<T>(parameters), name);
    //
    // public static IActorRef CreateActor<T>(this IUntypedActorContext context, string name, params object[] parameters) where T : ActorBase =>
    //     context.ActorOf(context.System.DependencyProps<T>(parameters), name);

    /// <summary>
    /// Nicely wrap the actor's ask pattern into EitherAsync&lt;ErrorInfo,T&gt;.
    /// </summary>
    /// <param name="actor">Target actor</param>
    /// <param name="message">A query message</param>
    /// <typeparam name="T">A success type</typeparam>
    /// <returns></returns>
    public static EitherAsync<Error,T> SafeAsk<T>(this ICanTell actor, object message) =>
        TryCatch(() => actor.Ask<Either<Error, T>>(message));

    public static Unit TellEx(this ICanTell target, object message, IActorRef? sender = null) {
        target.Tell(message, sender ?? ActorRefs.NoSender);
        return unit;
    }
}