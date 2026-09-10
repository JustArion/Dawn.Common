namespace Dawn.Common.Extensions;

public static class TaskEx
{
    extension<T>(Task<T> task)
    {
        [StackTraceHidden, DebuggerStepThrough]
        public T GetResult() => task.GetAwaiter().GetResult();
    }
}