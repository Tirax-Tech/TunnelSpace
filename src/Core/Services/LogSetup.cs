using Serilog;

namespace Tirax.TunnelSpace.Services;

public static class LogSetup
{
    static readonly Lazy<string> LocalAppFolder = new(() => {
        var isWindows = OperatingSystem.IsWindows();

        var path = Environment.GetFolderPath(isWindows
                                                 ? Environment.SpecialFolder.LocalApplicationData
                                                 : Environment.SpecialFolder.UserProfile);
        return Path.Combine(path, isWindows ? AppSettings.AppName : $".{AppSettings.AppName}");
    });

    static readonly string LogFolder;

    static LogSetup() {
        var folder = LocalAppFolder.Value;
        var logPath = Path.Combine(folder, "logs");
        if (!Directory.Exists(logPath))
            Directory.CreateDirectory(logPath);
        LogFolder = logPath;
    }

    static string GetLogFileName(string fileName) =>
        Path.Combine(LogFolder, fileName);

    public static ILogger Setup() =>
        new LoggerConfiguration()
           .WriteTo.Debug()
           .WriteTo.Console()
           .WriteTo.File(GetLogFileName("logs.txt"),
                         rollOnFileSizeLimit: true,
                         fileSizeLimitBytes: 512_000,
                         retainedFileCountLimit: 3)
           .CreateLogger();
}