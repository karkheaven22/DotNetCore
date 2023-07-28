using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using ILogger = Serilog.ILogger;

namespace LogHelper
{
    public static class LoggerExtensions
    {
        public static ILoggerFactory AddSeriLog(this ILoggerFactory loggerFactory, ILogger logger)
        {
            loggerFactory.AddProvider(new SerilogLoggerProvider(logger, false));
            return loggerFactory;
        }
        public static ILoggingBuilder AddSeriLog(this ILoggingBuilder factory, ILogger logger)
        {
            factory.AddProvider(new SerilogLoggerProvider(logger, false));
            return factory;
        }
    }
}
