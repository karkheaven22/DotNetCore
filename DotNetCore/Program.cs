using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using LogHelper;

namespace DotNetCore
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Log.Info("Program Start");
            CreateHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseWebRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddSeriLog(Log.Logger);
                    logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Information);
                    logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Information);
                    logging.AddEventSourceLogger();
                });
    }
}