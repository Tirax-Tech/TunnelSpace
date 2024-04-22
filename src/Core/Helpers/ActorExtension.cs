using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Dispatch;
using RZ.Foundation.Functional;
using CS = Akka.Actor.CoordinatedShutdown;
// ReSharper disable CheckNamespace

namespace RZ.Foundation.Akka;

public static class ActorExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Props DependencyProps<T>(this ActorSystem sys, params object[] parameters) where T : ActorBase =>
        DependencyResolver.For(sys).Props<T>(parameters);

    public static Task<Unit> CoordinatedShutdown(this ActorSystem system, Option<CS.Reason> reason = default) =>
        Task.Run(async () => {
                     await CS.Get(system).Run(reason.IfNone(CS.ClrExitReason.Instance));
                     return unit;
                 });

    public static IActorRef CreateActor<T>(this ActorSystem sys, string name, params object[] parameters) where T : ActorBase =>
        sys.ActorOf(sys.DependencyProps<T>(parameters), name);

    public static IActorRef CreateActor<T>(this IUntypedActorContext context, string name, params object[] parameters) where T : ActorBase =>
        context.ActorOf(context.System.DependencyProps<T>(parameters), name);

    public static Unit Respond<T>(this ICanTell target, Aff<T> message,
                                  Option<Func<Error, Error>> errorMapper = default,
                                  Option<IActorRef> sender = default) where T: notnull {
        ActorTaskScheduler.RunTask(async () => {
                                       var result = await message.IfFailAff(e => FailEff<T>(errorMapper.Apply(e).IfNone(e)))
                                                                 .IfFailAff(e => FailEff<T>(e.IsExceptional
                                                                                                ? e.Append(
                                                                                                    StandardErrors.StackTrace(
                                                                                                        e.ToException().StackTrace!))
                                                                                                : e))
                                                                 .Run();
                                       Outcome<T> final = result.ToEither();
                                       target.TellUnit(final, sender.ToNullable() ?? ActorRefs.NoSender);
                                   });
        return unit;
    }

    /// <summary>
    /// Nicely wrap the actor's ask pattern into EitherAsync&lt;ErrorInfo,T&gt;.
    /// </summary>
    /// <param name="actor">Target actor</param>
    /// <param name="message">A query message</param>
    /// <typeparam name="T">A success type</typeparam>
    /// <returns></returns>
    public static Aff<T> SafeAsk<T>(this ICanTell actor, object message) =>
        from result in Aff(async () => await actor.Ask<Outcome<T>>(message))
        from value in result.IfSuccess(out var v, out var e) ? SuccessEff(v) : FailEff<T>(e)
        select value;

    public static Unit TellUnit(this ICanTell target, object message, IActorRef? sender = null) {
        target.Tell(message, sender ?? ActorRefs.NoSender);
        return unit;
    }
}