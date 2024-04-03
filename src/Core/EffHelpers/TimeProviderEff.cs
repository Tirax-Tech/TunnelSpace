namespace Tirax.TunnelSpace.EffHelpers;

public sealed class TimeProviderEff(TimeProvider provider)
{
    public static readonly TimeProviderEff System = new(TimeProvider.System);

    public readonly Eff<DateTimeOffset> LocalNow = Eff(provider.GetLocalNow);
    public readonly Eff<DateTimeOffset> UtcNow = Eff(provider.GetUtcNow);
}