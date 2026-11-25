using System.Reactive.Disposables;
using Humanizer;
using MethodBoundaryAspect.Fody.Attributes;

namespace Dawn.Common;

[Serializable]
public class PerformanceMonitor(int warnDurationMs = -1) : OnMethodBoundaryAspect
{
    public override void OnEntry(MethodExecutionArgs args)
    {
        args.MethodExecutionTag = Stopwatch.GetTimestamp();
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        var ts = (long)args.MethodExecutionTag;
        var sw = Stopwatch.GetElapsedTime(ts);
        var method = args.Method;

        if ((int)sw.TotalMilliseconds == 0)
            return;

        var shouldWarn = warnDurationMs > 0 && sw.TotalMilliseconds > warnDurationMs;

        if (shouldWarn)
            Log.Warning("{Type}::{MethodName} took {Time} which is higher than the expected {Expected}ms", method.DeclaringType?.Name, method.Name, sw.Humanize(), warnDurationMs);
        else Log.Verbose("{Type}::{MethodName} took {Time}ms", method.DeclaringType?.Name, method.Name, sw.TotalMilliseconds);
    }
    
    public static IDisposable Monitor(Action<TimeSpan> callback)
    {
        var sw = Stopwatch.GetTimestamp();

        return Disposable.Create(() => callback(Stopwatch.GetElapsedTime(sw)));
    }
}