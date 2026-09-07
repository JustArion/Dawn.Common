using Serilog.Events;

namespace Dawn.Common.Serilog;

public class NullLogger : ILogger
{
    public static ILogger Instance { get; } = new NullLogger();
    private NullLogger() { }
    public void Write(LogEvent logEvent)
    {
        
    }

}