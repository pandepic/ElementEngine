using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace PandaEngine
{
    public static class Logging
    {
        public static Logger Logger { get; set; }

        public static void Load()
        {
            Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.File("log.txt",
                    LogEventLevel.Verbose,
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    null, 1073741824L, null, buffered: false, shared: false, null,
                    RollingInterval.Infinite, rollOnFileSizeLimit: false, 31)
                .CreateLogger();
        }
    }
}
