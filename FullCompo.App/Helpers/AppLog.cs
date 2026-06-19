using System;
using System.IO;

namespace FullCompo.App.Helpers;

public static class AppLog
{
    private static readonly object Lock = new();

    public static string LogsDirectory
    {
        get
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "FullCompo", "data", "Logs");
        }
    }

    private static string LogFilePath => Path.Combine(LogsDirectory, "app.log");

    public static void EnsureDirectory()
    {
        Directory.CreateDirectory(LogsDirectory);
    }

    public static void Write(string message)
    {
        try
        {
            EnsureDirectory();
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            lock (Lock)
            {
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must not crash the app.
        }
    }

    public static void WriteException(string context, Exception ex)
    {
        Write($"[{context}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
    }
}
