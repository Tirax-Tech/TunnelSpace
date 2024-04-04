namespace Tirax.TunnelSpace.Domain;

public static class ServerInputFormat
{
    static bool IsPortUnspecified(short port) => port == 0;

    public static Option<(string Host, short Port)> Parse(string input)
    {
        var parts = input.Split(':');
        return parts.Length switch
        {
            1 => Some((parts[0], (short) 0)),
            2 => from port in parseShort(parts[1])
                 select (parts[0], port),
            _ => None
        };
    }

    public static string ToSshServerFormatf(string host, short port) =>
        IsPortUnspecified(port) ? host : $"{host}:{port}";
}