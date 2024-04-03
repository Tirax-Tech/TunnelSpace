using LanguageExt.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Tirax.TunnelSpace.EffHelpers;

public readonly struct ServiceProviderEff(IServiceProvider sp)
{
    readonly Option<IServiceProvider> serviceProvider = Optional(sp);

    public static ServiceProviderEff Call(IServiceProvider sp) => new(sp);

    public Eff<Option<T>> GetService<T>() =>
        from sp in GetProvider()
        select Optional(sp.GetService<T>());

    public Eff<T> GetRequiredService<T>() where T : notnull =>
        from sp in GetProvider()
        select sp.GetRequiredService<T>();

    Eff<IServiceProvider> GetProvider() =>
        serviceProvider.ToEff(Error.New(ErrorCodes.NotFound, "Service provider not found"));
}