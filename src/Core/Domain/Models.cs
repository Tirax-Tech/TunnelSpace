namespace Tirax.TunnelSpace.Domain;

public sealed record TunnelConfig(
    Guid Id,
    string Host,
    short Port,
    short LocalPort,
    string RemoteHost,
    short RemotePort,
    string Name);