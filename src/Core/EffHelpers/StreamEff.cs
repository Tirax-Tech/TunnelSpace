using System.Runtime.CompilerServices;

namespace Tirax.TunnelSpace.EffHelpers;

public static class StreamEff
{
    public static Eff<StreamReader> NewStreamReader(Stream stream) =>
        Eff(() => new StreamReader(stream));

    public static Eff<StreamWriter> NewStreamWriter(Stream stream) =>
        Eff(() => new StreamWriter(stream));

    public static Eff<Unit> CloseEff(this TextWriter writer) =>
        eff(writer.Close);

    public static Aff<Unit> FlushAsyncEff(this TextWriter writer) =>
        aff(writer.FlushAsync);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Eff<string> ReadToEndEff(this TextReader reader) =>
        Eff(reader.ReadToEnd);

    public static Aff<string> ReadToEndAsyncEff(this TextReader reader) =>
        Aff(() => reader.ReadToEndAsync().ToValue());

    public static Aff<Unit> WriteAsyncEff(this TextWriter writer, string? value) =>
        aff(() => writer.WriteAsync(value));
}