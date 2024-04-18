using LanguageExt.Common;

namespace Tirax.TunnelSpace.EffHelpers;

public static class AppStandardErrors
{
    public const int NotFoundCode = 1_000_000;
    public const int DuplicatedCode = 1_000_001;

    public static Error NotFoundFromKey(string key) => (NotFoundCode, $"Key [{key}] is not found");

    public static readonly Error NotFound = (NotFoundCode, "Not Found");
    public static readonly Error Duplicated = (DuplicatedCode, "Item is duplicated");
}