using Serilog.Core;
using Serilog.Events;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace LogHelper
{
    internal class SerilogContextEnricher : ILogEventEnricher
    {
        public static string GetHostName()
        {
            return Dns.GetHostName();
        }

        public static string GetIPAddress(bool getIPV6 = false)
        {
            try
            {
                var ipAddresses = (Dns.GetHostAddresses(GetHostName())).ToList();

                if (!ipAddresses.Any())
                {
                    return string.Empty;
                }
                else
                {
                    ipAddresses = ipAddresses.Where(ip => !IPAddress.IsLoopback(ip)).ToList();
                }

                foreach (var ipAddress in ipAddresses)
                {
                    if (getIPV6 && ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        return ipAddress.ToString();
                    }
                    else if (!getIPV6 && ipAddress.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ipAddress.ToString();
                    }
                }
            }
            catch (Exception)
            {
                // Noncompliant
            }

            return string.Empty;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var applicationAssembly = Assembly.GetEntryAssembly();
            var name = applicationAssembly?.GetName().Name ?? "SeriLog";
            var version = applicationAssembly?.GetName().Version ?? new Version("0.0.0");
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ApplicationName", name));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ApplicationVersion", version));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("IpAddress", GetIPAddress(false)));
        }
    }
}