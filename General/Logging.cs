using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace ElementEngine
{
    public static class Logging
    {
        private static Logger _logger { get; set; }

        public static void Load()
        {
            _logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.File("log.txt",
                    LogEventLevel.Verbose,
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    null, 1073741824L, null, buffered: false, shared: false, null,
                    RollingInterval.Infinite, rollOnFileSizeLimit: false, 31)
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
    }
}
