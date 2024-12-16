namespace Tirax.TunnelSpace.Domain;

public sealed record TunnelConfig(
    string Host,
    short Port,
    short LocalPort,
    string RemoteHost,
    short RemotePort,
    string Name,
    Guid Id)
{
    public static TunnelConfig CreateSample() => new("localhost", 22, 8080, "localhost", 80, "Sample", Guid.Empty);
}