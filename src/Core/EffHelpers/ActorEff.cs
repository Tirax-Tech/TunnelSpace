﻿using System.Linq.Expressions;
using Akka.Actor;
using Akka.Configuration;
using LanguageExt.Common;

namespace Tirax.TunnelSpace.EffHelpers;

public static class ActorEff
{
    public static Aff<T> AskEff<T>(this ICanTell target, object message) =>
        Aff(() => target.Ask<T>(message).ToValue());

    public static Eff<IActorRef> CreateActor<T>(this IActorRefFactory context, Expression<Func<T>> creator, Option<string> name = default)
        where T : ActorBase =>
        from prop in SuccessEff(Props.Create(creator))
        from actor in Eff(() => context.ActorOf(prop, name.ToNullable()))
        select actor;

    public static Eff<ActorSystem> CreateActorSystem(string name, Config config) =>
        Eff(() => ActorSystem.Create(name, config));

    public static Eff<Unit> TellEff(this ICanTell target, object message, Option<IActorRef> sender = default) =>
        eff(() => target.Tell(message, sender.ToNullable()));

    public static Eff<Unit> Respond<T>(this ICanTell target, Eff<T> message,
                                          Option<Func<Error, Exception>> errorMapper = default,
                                          Option<IActorRef> sender = default) =>
        from result in message.Match<T,object>(v => v!, e => new Status.Failure(errorMapper.Apply(e).IfNone(e.ToException)))
        from _ in target.TellEff(result, sender)
        select unit;
}