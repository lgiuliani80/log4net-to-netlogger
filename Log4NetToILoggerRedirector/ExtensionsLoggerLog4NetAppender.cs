using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Logging;

class PropertiesWithMessage : Dictionary<string, object?>
{
    public override string ToString() => this["Message"]?.ToString() ?? string.Empty;
}

public class ExtensionsLoggerLog4NetAppender : AppenderSkeleton
{
    private readonly ILoggerFactory _lf;

    public ExtensionsLoggerLog4NetAppender(ILoggerFactory lf)
    {
        _lf = lf;

        var hierarchy = (Hierarchy)LogManager.GetRepository();
        hierarchy.Root.AddAppender(this);
    }

    private static LogLevel GetLogLevel(Level? level)
    {
        return level?.Name switch
        {
            "OFF" => LogLevel.None,
            "FATAL" or "EMERGENCY" or "ALERT" or "CRITICAL" or "SEVERE" => LogLevel.Critical,
            "ERROR" => LogLevel.Error,
            "WARN" or "WARNING" => LogLevel.Warning,
            "INFO" or "NOTICE" => LogLevel.Information,
            "DEBUG" or "FINE" => LogLevel.Debug,
            "TRACE" or "FINER" or "FINEST" or "VERBOSE" => LogLevel.Trace,
            _ => LogLevel.Debug
        };
    }

    protected override void Append(LoggingEvent loggingEvent)
    {
        var obj = loggingEvent.GetLoggingEventData();

        var content = new PropertiesWithMessage
        {
            ["Message"] = loggingEvent.RenderedMessage,
            ["Log4NetLevel"] = loggingEvent.Level?.Name,
            ["Log4NetLoggerName"] = loggingEvent.LoggerName,
            ["Log4NetTimeStamp"] = loggingEvent.TimeStamp,
            ["Log4NetThreadName"] = loggingEvent.ThreadName,
            ["Log4NetDomain"] = loggingEvent.Domain,
            ["Log4NetIdentity"] = loggingEvent.Identity,
            ["Log4NetUserName"] = loggingEvent.UserName
        };

        foreach (var p in obj.Properties?.GetKeys() ?? [])
        {
            content[p] = obj.Properties![p];
        }

        var logger = _lf.CreateLogger(loggingEvent.LoggerName ?? "Program");
        logger.Log(GetLogLevel(loggingEvent.Level), new EventId(), content, loggingEvent.ExceptionObject, (state, ex) => state.ToString());
    }
}

public static class ExtensionsLoggerLog4NetAppenderExtensions
{
    public static void AddLog4NetRedirector(this IServiceCollection collection)
    {
        collection.AddActivatedSingleton<ExtensionsLoggerLog4NetAppender>();
    }
}
