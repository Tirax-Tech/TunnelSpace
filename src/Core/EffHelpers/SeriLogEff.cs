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
}