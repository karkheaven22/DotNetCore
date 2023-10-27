using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Settings.Configuration;
using Serilog.Sinks.SystemConsole.Themes;

namespace LogHelper.Logger
{
    internal sealed class FileLogger
    {
        private bool IsLoggingEnabled;
        private bool IsConsoleEnabled;
        private IConfiguration? Configuration;
        private static readonly Lazy<FileLogger> lazyInstance = new(true);
        public ILogger Logger = null!;
        public static FileLogger Instance => lazyInstance.Value;

        public FileLogger()
        {
            Initialize(null);
        }

        public FileLogger(IConfiguration? configuration)
        {
            Initialize(configuration);
        }

        private void Initialize(IConfiguration? configuration)
        {
            Configuration = configuration ?? DefaultConfiguration();
            IsLoggingEnabled = Convert.ToBoolean(Configuration.GetSection("LoggingEnabled").Value);
            IsConsoleEnabled = Convert.ToBoolean(Configuration.GetSection("ConsoleEnabled").Value);
            Logger = CreateLogger();
        }

        private static IConfiguration DefaultConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return new ConfigurationBuilder()
                .AddJsonFile($"appsettings.json", true, true)
                .AddJsonFile($"appsettings.{environment}.json", true, true)
                .Build();
        }

        public ILogger CreateLogger()
        {
            var options = new ConfigurationReaderOptions { SectionName = "Filelog" };
            return new LoggerConfiguration()
                    .Filter.ByExcluding(_ => !IsLoggingEnabled)
                    .MinimumLevel.Debug()
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId()
                    .Enrich.With(new SerilogContextEnricher())
                    .WriteTo.Conditional(evt => IsConsoleEnabled, wt => wt.Console(theme: SystemConsoleTheme.Literate))
                    .ReadFrom.Configuration(Configuration!, options)
                    .CreateLogger();
        }

        public void UseForContext<TSource>()
        {
            Logger = Logger.ForContext<TSource>();
        }
    }
}