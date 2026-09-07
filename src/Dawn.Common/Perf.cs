using System.Reactive.Disposables;

namespace Dawn.Common;

public static class Perf
{
    public static IDisposable Monitor(Action<TimeSpan> callback)
    {
        var sw = Stopwatch.GetTimestamp();

        return Disposable.Create(() => callback(Stopwatch.GetElapsedTime(sw)));
    }
}