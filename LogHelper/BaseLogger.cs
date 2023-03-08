using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using System.Security;
using System.Text.Json;
using ILogger = Serilog.ILogger;

namespace LogHelper
{
    internal static class FileLogger
    {
        private static IConfiguration Configuration { get; } = CreateConfiguration();
        public static bool IsLoggingEnabled => Configuration.GetValue<bool>("LoggingEnabled");

        private static IConfiguration CreateConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            try
            {
                builder.Build();
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is JsonException || ex is SecurityException)
            {
                Console.WriteLine($"An exception of type {ex.GetType()} occurred: {ex.Message}");
            }

            return builder.Build();
        }

        private static ILogger CreateLogger()
        {
            return new LoggerConfiguration()
                    .Filter.ByExcluding(_ => !IsLoggingEnabled)
                    .MinimumLevel.Debug()
                    .Enrich.FromLogContext()
                    .Enrich.WithEnvironmentName()
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId()
                    .Enrich.With(new SerilogContextEnricher())
                    .ReadFrom.Configuration(Configuration, "Filelog")
                    .WriteTo.Console()
                    .CreateLogger();
        }

        public static ILogger Logger => CreateLogger();
    }

    public static class Log
    {
        public static bool IsLoggingEnabled => FileLogger.IsLoggingEnabled;
        public static ILogger Logger => FileLogger.Logger;

        public static ILogger ForContext<T>()
        {
            return Logger.ForContext<T>();
        }

        public static ILoggerFactory AddSeriLog(this ILoggerFactory loggerFactory)
        {
            loggerFactory.AddProvider(new SerilogLoggerProvider(Logger, false));
            return loggerFactory;
        }

        public static ILoggingBuilder AddSeriLog(this ILoggingBuilder factory)
        {
            factory.AddProvider(new SerilogLoggerProvider(Logger, false));
            return factory;
        }

        public static string TrimLine(this string message) => message.Replace("\n", String.Empty).Replace("\r", String.Empty);

        public static void Debug(this ILogger Logger, string message) => Logger.Debug(message);

        public static void Debug(string message) => Logger.Debug(message);

        public static void Debug(string message, params object[] propertyValues) => Logger.Debug(message, propertyValues);

        public static void Debug(Exception exception, string message, params object[] propertyValues) => Logger.Debug(exception, message, propertyValues);

        public static void Info(this ILogger Logger, string message) => Logger.Information(message);

        public static void Info(string message) => Logger.Information(message);

        public static void Info(string message, params object[] propertyValues) => Logger.Information(message, propertyValues);

        public static void Info(Exception exception, string message, params object[] propertyValues) => Logger.Information(exception, message, propertyValues);

        public static void Warn(this ILogger Logger, string message) => Logger.Warning(message);

        public static void Warn(string message) => Logger.Warning(message);

        public static void Warn(string message, params object[] propertyValues) => Logger.Warning(message, propertyValues);

        public static void Warn(Exception exception, string message, params object[] propertyValues) => Logger.Warning(exception, message, propertyValues);

        public static void Error(this ILogger Logger, string message) => Logger.Error(message);

        public static void Error(string message) => Logger.Error(message);

        public static void Error(string message, params object[] propertyValues) => Logger.Error(message, propertyValues);

        public static void Error(Exception exception, string message, params object[] propertyValues) => Logger.Error(exception, message, propertyValues);

        public static void Fatal(this ILogger Logger, string message) => Logger.Fatal(message);
        public static void Fatal(string message) => Logger.Fatal(message);

        public static void Fatal(string message, params object[] propertyValues) => Logger.Fatal(message, propertyValues);

        public static void Fatal(Exception exception, string message, params object[] propertyValues) => Logger.Fatal(exception, message, propertyValues);
    }
}