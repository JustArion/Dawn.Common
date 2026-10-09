namespace Dawn.Common.Extensions;

public static class TaskEx
{
    extension<T>(Task<T> task)
    {
        [StackTraceHidden, DebuggerStepThrough]
        public T GetResult() => task.GetAwaiter().GetResult();
        
        public Task<T?> Catch(Action<Exception> handler)
        {
            return task.ContinueWith(completeTask =>
            {
                if (completeTask is not { IsFaulted: true, Exception: { } ex }) 
                    return completeTask.Result;
                
                handler(ex);
                return default;
            });
        }
    }
}