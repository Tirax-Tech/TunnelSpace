namespace Tirax.TunnelSpace.Domain;

public sealed record TunnelConfig(
    Guid Id,
    string Host,
    short Port,
    short LocalPort,
    string RemoteHost,
    short RemotePort,
    string Name)
{
    public static readonly TunnelConfig Sample = new(Guid.Empty, "localhost", 22, 8080, "localhost", 80, "Sample");
}