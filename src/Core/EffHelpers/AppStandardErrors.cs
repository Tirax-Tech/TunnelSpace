using LanguageExt.Common;

namespace Tirax.TunnelSpace.EffHelpers;

public static class AppStandardErrors
{
    public const int NotFoundCode = 1_000_000;
    public const int DuplicatedCode = 1_000_001;

    /// <summary>
    /// Code reaches a state that should not be possible by domain rules (indicate a bug in the code)
    /// </summary>
    public const int UnexpectedCode = 1_000_002;

    public static Error NotFoundFromKey(string key) => (NotFoundCode, $"Key [{key}] is not found");
    public static Error UnexpectedError(string message)  => (UnexpectedCode, message);

    public static readonly Error NotFound = (NotFoundCode, "Not Found");
    public static readonly Error Duplicated = (DuplicatedCode, "Item is duplicated");
    public static readonly Error Unexpected = (UnexpectedCode, "Unexpected error");
}