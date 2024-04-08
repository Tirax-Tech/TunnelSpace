using Serilog;
using Tirax.TunnelSpace.EffHelpers;

namespace Tirax.TunnelSpace.Services;

public static class LogSetup
{
    static readonly Eff<string> LocalAppFolder =
        from isWindows in OperatingSystemEff.IsWindows
        from path in EnvironmentEff.GetFolderPath(isWindows
                                                      ? Environment.SpecialFolder.LocalApplicationData
                                                      : Environment.SpecialFolder.UserProfile)
        select Path.Combine(path, isWindows ? AppSettings.AppName : $".{AppSettings.AppName}");

    static readonly Lazy<string> LogFolder =
        new(() => (from folder in LocalAppFolder
                   from path in Eff(() => {
                       var logPath = Path.Combine(folder, "logs");
                       if (!Directory.Exists(logPath))
                           Directory.CreateDirectory(logPath);
                       return logPath;
                   })
                   select path
                  ).Run()
                   .ThrowIfFail());

    static string GetLogFileName(string fileName) =>
        Path.Combine(LogFolder.Value, fileName);

    public static readonly Eff<ILogger> Setup =
        Eff(() => (ILogger) new LoggerConfiguration()
                            .WriteTo.Debug()
                            .WriteTo.Console()
                            .WriteTo.File(GetLogFileName("logs.txt"),
                                          rollOnFileSizeLimit: true, fileSizeLimitBytes: 512_000, retainedFileCountLimit: 3)
                            .CreateLogger());
}