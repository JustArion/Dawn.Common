using Dawn.Common.Exceptions;

namespace Dawn.Common;

public sealed record Error(string Message, Exception? Exception = null)
{
    public static implicit operator Error(string msg) => new(msg);
    public static implicit operator Error(Exception ex) => new(ex.Message, ex);
    
    public Exception ToException() => Exception ?? new ResultException(Message);
    public override string ToString() => Message;
}

public union Result<T>(T, Error)
{
    public static implicit operator T(Result<T> result) => result.Unwrap();

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error error) => new (error);
    public static implicit operator Result<T>(Exception ex) => (Error)ex;
    
    public bool IsSuccess => this is T;

    public T Unwrap() => this switch
    {
        T value => value,
        Error error => throw error.ToException(),
        _ => throw new ArgumentOutOfRangeException()
    };

    public T UnwrapOr(T fallback) => this switch
    {
        T value => value,
        _ => fallback,
    };

    public Result<TResult> Map<TResult>(Func<T, TResult> map) => this switch
    {
        T value => map(value),
        Error error => error,
        _ => throw new ArgumentOutOfRangeException(nameof(map))
    };

    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> bind) => this switch
    {
        T value => bind(value),
        Error error => error,
        _ => throw new ArgumentOutOfRangeException(nameof(bind))
    };
    
    public Result<TResult> Transfer<TResult>() => this switch
    {
        Error error => error,
        _ => throw new InvalidOperationException("Result is successful; use Map or Bind to transform the value."),
    };

    public override string ToString() => Value?.ToString() ?? string.Empty;

    public void Dispose()
    {
        if (Value is IDisposable disposable)
            disposable.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (Value is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
    }
}

public static class Result
{
    public static Result<T> Ok<T>(T value) => value;
    public static Result<T> Fail<T>(string message, Exception? ex = null) => new Error(message, ex);
}

