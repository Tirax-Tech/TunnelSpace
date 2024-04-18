using LanguageExt.Common;
using Serilog;
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace Tirax.TunnelSpace.EffHelpers;

public static class SeriLogEff
{
    public static Eff<Unit> InformationEff(this ILogger logger, string message) =>
        eff(() => logger.Information(message));

    public static Eff<Unit> InformationEff<T>(this ILogger logger, string message, T v) =>
        eff(() => logger.Information(message, v));

    public static Eff<Unit> InformationEff<T1,T2>(this ILogger logger, string message, T1 v1, T2 v2) =>
        eff(() => logger.Information(message, v1, v2));

    public static Eff<Unit> ErrorEff(this ILogger logger, Error error, string message) =>
        eff(() => logger.Error(error, message));

    public static Eff<Unit> ErrorEff<T1>(this ILogger logger, Error error, string message, T1 v1) =>
        eff(() => logger.Error(error, message, v1));

    public static Eff<Unit> ErrorEff<T1, T2>(this ILogger logger, Error error, string message, T1 v1, T2 v2) =>
        eff(() => logger.Error(error, message, v1, v2));

    public static Eff<Unit> ErrorEff<T1, T2, T3>(this ILogger logger, Error error, string message, T1 v1, T2 v2, T3 v3) =>
        eff(() => logger.Error(error, message, v1, v2, v3));

    public static Aff<Unit> LogResult<T>(this ILogger logger, Aff<T> work, Option<string> success = default, Option<Func<Error,string>> error = default) =>
        work.MatchAff(_ => success.Match(s => logger.InformationEff(s).ToAff(), unitAff),
                      e => logger.ErrorEff(e, error.Map(f => f(e)).IfNone("Error in LogResult!")));
}