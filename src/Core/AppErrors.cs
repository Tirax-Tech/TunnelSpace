using LanguageExt.Common;

namespace Tirax.TunnelSpace;

public static class AppErrors
{
    public static readonly Error ControllerNotStarted = (1, "SSH Controller is not started");
    public static readonly Error InvalidData = (2, "Invalid data");
}