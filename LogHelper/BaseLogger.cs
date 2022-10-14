using Microsoft.Extensions.Configuration;
using Serilog;
using System;

namespace LogHelper
{
    internal static class FileLogger
    {
        static IConfiguration? Configuration { get; set; }
        public static bool IsLoggingEnabled => Convert.ToBoolean(Configuration?.GetSection("LoggingEnabled").Value);
        static FileLogger() => EnvironmentCheck();
        private static void EnvironmentCheck()
        {
            try
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (!string.IsNullOrEmpty(environment))
                    Configuration = new ConfigurationBuilder()
                        .AddJsonFile($"appsettings.json", true, true)
                        //.AddJsonFile($"appsettings.{environment}.json", true, true)
                        .AddEnvironmentVariables()
                        .Build();
                else
                    Configuration = new ConfigurationBuilder()
                        .AddJsonFile($"appsettings.json", true, true)
                        .AddEnvironmentVariables()
                        .Build();
            }
            catch (Exception)
            {
                // Noncompliant
            }
        }
        public static ILogger CreateLogger()
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
                    .CreateLogger();
        }
        public static ILogger Logger => CreateLogger();
    }

    public static class Log
    {
        public static bool IsLoggingEnabled => FileLogger.IsLoggingEnabled;
        public static ILogger Logger => FileLogger.Logger;
        public static void Debug(string message) => FileLogger.Logger.Debug(message);
        public static void Debug(string message, params object[] propertyValues) => FileLogger.Logger.Debug(message, propertyValues);
        public static void Debug(Exception exception, string message, params object[] propertyValues) => FileLogger.Logger.Debug(exception, message, propertyValues);
        public static void Info(string message) => FileLogger.Logger.Information(message);
        public static void Info(string message, params object[] propertyValues) => FileLogger.Logger.Information(message, propertyValues);
        public static void Info(Exception exception, string message, params object[] propertyValues) => FileLogger.Logger.Information(exception, message, propertyValues);
        public static void Warn(string message) => FileLogger.Logger.Warning(message);
        public static void Warn(string message, params object[] propertyValues) => FileLogger.Logger.Warning(message, propertyValues);
        public static void Warn(Exception exception, string message, params object[] propertyValues) => FileLogger.Logger.Warning(exception, message, propertyValues);
        public static void Error(string message) => FileLogger.Logger.Error(message);
        public static void Error(string message, params object[] propertyValues) => FileLogger.Logger.Error(message, propertyValues);
        public static void Error(Exception exception, string message, params object[] propertyValues) => FileLogger.Logger.Error(exception, message, propertyValues);
        public static void Fatal(string message) => FileLogger.Logger.Fatal(message);
        public static void Fatal(string message, params object[] propertyValues) => FileLogger.Logger.Fatal(message, propertyValues);
        public static void Fatal(Exception exception, string message, params object[] propertyValues) => FileLogger.Logger.Fatal(exception, message, propertyValues);
    }
}