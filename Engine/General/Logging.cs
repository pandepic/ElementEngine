using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;
using System.IO;

namespace ElementEngine
{
    public static class Logging
    {
        public static Logger Logger { get; set; }

        /// <summary>
        /// Create a basic logger configuration you can pass to Load that will log to the console and the provided logFilePath
        /// </summary>
        public static LoggerConfiguration CreateBasicConfig(string logFilePath)
        {
            return new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.File(logFilePath,
                    LogEventLevel.Verbose,
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");
        }

        public static void Load(LoggerConfiguration config)
        {
            Logger = config.CreateLogger();
        }

        public static void Dispose()
        {
            Logger?.Dispose();
            Logger = null;
        }

        [Conditional("DEBUG")]
        public static void DebugBreak()
        {
            Debugger.Break();
        }

        [Conditional("DEBUG")]
        public static void Debug(string messageTemplate)
        {
            Logger?.Information(messageTemplate);
        }

        [Conditional("DEBUG")]
        public static void Debug(string messageTemplate, params object[] propertyValues)
        {
            Logger?.Information(messageTemplate, propertyValues);
        }

        public static void Information(string messageTemplate)
        {
            Logger?.Information(messageTemplate);
        }

        public static void Information(string messageTemplate, params object[] propertyValues)
        {
            Logger?.Information(messageTemplate, propertyValues);
        }

        public static void Error(string messageTemplate)
        {
            Logger?.Error(messageTemplate);
        }

        public static void Error(string messageTemplate, params object[] propertyValues)
        {
            Logger?.Error(messageTemplate, propertyValues);
        }

        public static void Warning(string messageTemplate)
        {
            Logger?.Warning(messageTemplate);
        }

        public static void Warning(string messageTemplate, params object[] propertyValues)
        {
            Logger?.Warning(messageTemplate, propertyValues);
        }

        public static void Fatal(string messageTemplate)
        {
            Logger?.Fatal(messageTemplate);
        }

        public static void Fatal(string messageTemplate, params object[] propertyValues)
        {
            Logger?.Fatal(messageTemplate, propertyValues);
        }
    }
}
