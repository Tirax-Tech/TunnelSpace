using LanguageExt.Common;
using Serilog;
// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace Tirax.TunnelSpace.EffHelpers;

public static class SeriLogEff
{
    public static Eff<Unit> InformationEff(this ILogger logger, string message) =>
        eff(() => logger.Information(message));

    public static Eff<Unit> ErrorEff(this ILogger logger, Error error, string message) =>
        eff(() => logger.Error(error, message));
}