using System.Runtime.CompilerServices;

namespace Dawn.Common;

public static class Static
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SuppressExceptions(Action act) => SuppressExceptions<Exception>(act);
    
    public static void SuppressExceptions<T>(Action act) where T : Exception
    {
        try
        {
            act();
        }
        catch (Exception ex)
        {
            if (typeof(T) == typeof(Exception))
                return;
            
            if (ex.GetType() != typeof(T))
                throw;
            // ignored
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? SuppressExceptions<T>(Func<T> func) => SuppressExceptions<T, Exception>(func); 
    
    public static T? SuppressExceptions<T, TException>(Func<T> func) where TException : Exception
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            if (typeof(TException) == typeof(Exception))
                return default;
            
            if (ex.GetType() != typeof(TException))
                throw;
            
            return default;
        }
    }
}