using System.Runtime.CompilerServices;
using Serilog;

namespace Tirax.TunnelSpace.EffHelpers;

public readonly struct LoggerEff(ILogger logger)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LoggerEff Call(ILogger logger) => new(logger);

    public Eff<Unit> Information(string message) {
        var l = logger;
        return Eff(() => {
            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
            l.Information(message);
            return unit;
        });
    }

    public Eff<Unit> Information<T>(string message, T data) {
        var l = logger;
        return Eff(() => {
            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
            l.Information(message, data);
            return unit;
        });
    }
}