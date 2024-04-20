namespace Tirax.TunnelSpace.Helpers;

public interface IUniqueId
{
    Guid NewGuid();
}

public class UniqueId : IUniqueId
{
    public Guid NewGuid() => Guid.NewGuid();
}