using System.Text;

namespace TipMolde.Diagnostics;

internal static class StartupCrashLogger
{
    private static int _registered;

    private static string LogDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TipMolde", "logs");

    public static string LogFilePath => Path.Combine(LogDirectory, "startup-crash.log");

    public static void RegisterGlobalHandlers()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
            return;

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            Log("AppDomain.CurrentDomain.UnhandledException", exception?.ToString() ?? args.ExceptionObject?.ToString() ?? "Unknown exception object");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log("TaskScheduler.UnobservedTaskException", args.Exception.ToString());
        };
    }

    public static void LogException(string stage, Exception exception)
    {
        Log(stage, exception.ToString());
    }

    private static void Log(string stage, string content)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);

            var builder = new StringBuilder();
            builder.AppendLine("==================================================");
            builder.AppendLine(DateTimeOffset.Now.ToString("O"));
            builder.AppendLine(stage);
            builder.AppendLine(content);

            File.AppendAllText(LogFilePath, builder.ToString());
        }
        catch
        {
            // Logging must never crash startup.
        }
    }
}
