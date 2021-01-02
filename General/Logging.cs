using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.IO;

namespace ElementEngine
{
    public static class Logging
    {
        private static Logger _logger { get; set; }

        public static void Load()
        {
            File.Delete("log.txt");

            _logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.File("log.txt",
                    LogEventLevel.Verbose,
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        public static void Information(string messageTemplate)
        {
            _logger.Information(messageTemplate);
        }

        public static void Information(string messageTemplate, params object[] propertyValues)
        {
            _logger.Information(messageTemplate, propertyValues);
        }

        public static void Error(string messageTemplate)
        {
            _logger.Error(messageTemplate);
        }

        public static void Error(string messageTemplate, params object[] propertyValues)
        {
            _logger.Error(messageTemplate, propertyValues);
        }

        public static void Warning(string messageTemplate)
        {
            _logger.Warning(messageTemplate);
        }

        public static void Warning(string messageTemplate, params object[] propertyValues)
        {
            _logger.Warning(messageTemplate, propertyValues);
        }

        public static void Fatal(string messageTemplate)
        {
            _logger.Fatal(messageTemplate);
        }

        public static void Fatal(string messageTemplate, params object[] propertyValues)
        {
            _logger.Fatal(messageTemplate, propertyValues);
        }
    }
}
