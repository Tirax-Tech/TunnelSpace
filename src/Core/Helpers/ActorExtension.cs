using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Dispatch;
using CS = Akka.Actor.CoordinatedShutdown;
// ReSharper disable CheckNamespace

namespace RZ.Foundation.Akka;

public static class ActorExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static Props DependencyProps<T>(this ActorSystem sys, params object[] parameters) where T : ActorBase =>
        DependencyResolver.For(sys).Props<T>(parameters);

    public static Task CoordinatedShutdown(this ActorSystem system, CS.Reason? reason = null)
        => CS.Get(system).Run(reason ?? CS.ClrExitReason.Instance);

    public static IActorRef CreateActor<T>(this ActorSystem sys, string name, params object[] parameters) where T : ActorBase =>
        sys.ActorOf(sys.DependencyProps<T>(parameters), name);

    public static IActorRef CreateActor<T>(this IUntypedActorContext context, string name, params object[] parameters) where T : ActorBase =>
        context.ActorOf(context.System.DependencyProps<T>(parameters), name);

    public static void Respond(this ICanTell target, Task message,
                                  Func<Exception, Exception>? errorMapper = null,
                                  IActorRef? sender = null) {
        ActorTaskScheduler.RunTask(async () => {
            var error = await Try(message, identity);
            var finalSender = sender ?? ActorRefs.NoSender;
            if (error is null)
                target.Tell(unit, finalSender);
            else{
                var final = errorMapper?.Invoke(error) ?? error;
                target.Tell(new Status.Failure(final), finalSender);
            }
        });
    }

    public static void Respond<T>(this ICanTell target, Task<T> message,
                                  Func<Exception, Exception>? errorMapper = null,
                                  IActorRef? sender = null) where T : notnull {
        ActorTaskScheduler.RunTask(async () => {
            var (error, result) = await Try(message, identity);
            var finalSender = sender ?? ActorRefs.NoSender;
            if (error is null)
                target.Tell(result, finalSender);
            else{
                var final = errorMapper?.Invoke(error) ?? error;
                target.Tell(new Status.Failure(final), finalSender);
            }
        });
    }

    public static void TellUnit(this ICanTell target, IActorRef? sender = null) {
        target.Tell(unit, sender ?? ActorRefs.NoSender);
    }
}