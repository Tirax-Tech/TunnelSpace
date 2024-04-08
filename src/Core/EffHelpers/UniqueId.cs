namespace Tirax.TunnelSpace.EffHelpers;

public interface IUniqueId
{
    Eff<Guid> NewGuid { get; }
}

public class UniqueId : IUniqueId
{
    public Eff<Guid> NewGuid { get; } = Eff(Guid.NewGuid);
}