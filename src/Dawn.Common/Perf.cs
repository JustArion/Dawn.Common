using System.Reactive.Disposables;
using Humanizer;
using MethodBoundaryAspect.Fody.Attributes;

namespace Dawn.Common;

[Serializable]
public class PerfAttribute(int warnDurationMs = -1) : OnMethodBoundaryAspect
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
            
            
        Log.Verbose("{Type}::{MethodName} took {Time}", method.DeclaringType?.Name, method.Name, sw.Humanize());

        if (warnDurationMs == -1)
            return;

        if (!(sw.TotalMilliseconds > warnDurationMs)) 
            return;
            
        Log.Warning("{Type}::{MethodName} took {Time} which is higher than the expected {Expected} milliseconds", method.DeclaringType?.Name, method.Name, sw.Humanize(), warnDurationMs);
    }
    
    public static IDisposable Monitor(Action<TimeSpan> callback)
    {
        var sw = Stopwatch.GetTimestamp();

        return Disposable.Create(() => callback(Stopwatch.GetElapsedTime(sw)));
    }
}