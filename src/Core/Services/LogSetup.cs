using Serilog;
using Tirax.TunnelSpace.EffHelpers;

namespace Tirax.TunnelSpace.Services;

public static class LogSetup
{
    static readonly string LocalAppFolder =
        (from isWindows in OperatingSystemEff.IsWindows
         from path in EnvironmentEff.GetFolderPath(isWindows
                                                       ? Environment.SpecialFolder.LocalApplicationData
                                                       : Environment.SpecialFolder.UserProfile)
         select Path.Combine(path, isWindows ? AppSettings.AppName : $".{AppSettings.AppName}")
        ).Run().ThrowIfFail();

    static readonly Lazy<string> LogFolder = new(() => {
        var logPath = Path.Combine(LocalAppFolder, "logs");
        if (!Directory.Exists(logPath))
            Directory.CreateDirectory(logPath);
        return logPath;
    });

    static string GetLogFileName(string fileName) =>
        Path.Combine(LogFolder.Value, fileName);

    static string CreateLogFileName(DateTimeOffset now) =>
        GetLogFileName($"log-{now:yyyyMMdd-HHmmss}.txt");

    public static Eff<ILogger> Setup(DateTimeOffset now) =>
        Eff(() => (ILogger) new LoggerConfiguration()
                            .WriteTo.Debug()
                            .WriteTo.File(CreateLogFileName(now),
                                          rollOnFileSizeLimit: true, fileSizeLimitBytes: 512_000, retainedFileCountLimit: 3)
                            .CreateLogger());
}