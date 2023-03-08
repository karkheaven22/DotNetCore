using Serilog.Core;
using Serilog.Events;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace LogHelper
{
    internal class SerilogContextEnricher : ILogEventEnricher
    {
        private static readonly string _applicationName = Assembly.GetEntryAssembly()?.GetName().Name ?? "SeriLog";
        private static readonly string _applicationVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";
        private static readonly string _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        public static string GetHostName()
        {
            return Dns.GetHostName();
        }

        public static string GetIPAddress(bool useIPv6 = false)
        {
            var ipAddresses = Dns.GetHostAddresses(GetHostName()).Where(ip => !IPAddress.IsLoopback(ip));

            var ipAddress = useIPv6 ?
                ipAddresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetworkV6) :
                ipAddresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

            return ipAddress?.ToString() ?? string.Empty;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {

            logEvent.AddPropertyIfAbsent(new LogEventProperty("ApplicationName", new ScalarValue(_applicationName)));
            logEvent.AddPropertyIfAbsent(new LogEventProperty("ApplicationVersion", new ScalarValue(_applicationVersion)));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("IpAddress", GetIPAddress(false)));
            logEvent.AddPropertyIfAbsent(new LogEventProperty("EnvironmentName", new ScalarValue(_environment)));
            logEvent.AddPropertyIfAbsent(new LogEventProperty("MachineName", new ScalarValue(Environment.MachineName)));
            logEvent.AddPropertyIfAbsent(new LogEventProperty("ThreadId", new ScalarValue(Environment.CurrentManagedThreadId)));
        }
    }
}