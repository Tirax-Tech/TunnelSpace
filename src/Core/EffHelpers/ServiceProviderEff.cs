using Microsoft.Extensions.DependencyInjection;

namespace Tirax.TunnelSpace.EffHelpers;

public static class ServiceProviderEffect
{
    public static Eff<Option<T>> GetServiceEff<T>(this IServiceProvider sp) =>
        Eff(() => Optional(sp.GetService<T>()));

    public static Eff<T> GetRequiredServiceEff<T>(this IServiceProvider sp) where T : notnull =>
        Eff(sp.GetRequiredService<T>);

}