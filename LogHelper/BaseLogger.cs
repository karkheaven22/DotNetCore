using LogHelper.Logger;
using ILogger = Serilog.ILogger;

namespace LogHelper
{
    public static class Log
    {
        public static ILogger Logger => FileLogger.Instance.Logger;

        public static void Debug(string message) => Logger.Debug(message);

        public static void Debug(Exception exception, string message) => Logger.Debug(exception, message);

        public static void Info(string message) => Logger.Information(message);

        public static void Info(Exception exception, string message) => Logger.Information(exception, message);

        public static void Warn(string message) => Logger.Warning(message);

        public static void Warn(Exception exception, string message) => Logger.Warning(exception, message);

        public static void Error(string message) => Logger.Error(message);

        public static void Error(Exception exception, string message) => Logger.Error(exception, message);

        public static void Fatal(string message) => Logger.Fatal(message);

        public static void Fatal(Exception exception, string message) => Logger.Fatal(exception, message);
    }
}