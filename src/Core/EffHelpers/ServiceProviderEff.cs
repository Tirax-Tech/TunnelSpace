using Microsoft.Extensions.DependencyInjection;

namespace Tirax.TunnelSpace.EffHelpers;

public sealed class ServiceProviderEff(IServiceProvider serviceProvider)
{
    public static ServiceProviderEff Call(IServiceProvider sp) => new(sp);

    public Eff<Option<T>> GetService<T>() {
        var sp = serviceProvider;
        return Eff(() => Optional(sp.GetService<T>()));
    }

    public Eff<T> GetRequiredService<T>() where T : notnull =>
        Eff(serviceProvider.GetRequiredService<T>);
}