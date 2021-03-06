using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public static class FileLoggerExtensions
{
    //add 日志文件创建规则，分割规则，格式化规则，过滤规则 to appsettings.json
    public static ILoggerFactory AddFile(this ILoggerFactory factory, IConfiguration configuration)
    {
        return AddFile(factory, new FileLoggerSettings(configuration));
    }

    public static ILoggerFactory AddFile(this ILoggerFactory factory, FileLoggerSettings fileLoggerSettings)
    {
        factory.AddProvider(new FileLoggerProvider(fileLoggerSettings));
        return factory;
    }

    public static ILoggingBuilder AddFile(this ILoggingBuilder factory, IConfiguration configuration)
    {
        return AddFile(factory, new FileLoggerSettings(configuration));
    }

    public static ILoggingBuilder AddFile(this ILoggingBuilder factory, FileLoggerSettings fileLoggerSettings)
    {
        factory.AddProvider(new FileLoggerProvider(fileLoggerSettings));
        return factory;
    }
}