using System.Diagnostics.CodeAnalysis;
using Dawn.Common.Exceptions;

namespace Dawn.Common;

public union Result(bool, Error)
{
    public static Result Failed(Error error) => error;
}

public union Result<T>(T, bool, Error) : IAsyncDisposable, IDisposable
{
    public static Result<T> Transfer<T1>(Result<T1> resultA) => (Error)resultA.Value!;

    public bool Success => Value is T;
    public static implicit operator Result<T>(Result result) =>
        result switch
        {
            bool b => b,
            Error error => error,
        };

    public static implicit operator T(Result<T> result)
    {
        switch (result.Value)
        {
            case T val:
                return val;
            case Error error:
                switch (error)
                {
                    case string str:
                        throw new ResultException(str);
                    case Exception ex:
                        throw ex;
                }

                break;
        }

        throw new NullReferenceException(nameof(result.Value));
    }
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

public union Error(string, Exception)
{
    public override string ToString() => Value?.ToString() ?? string.Empty; 
    public static Error Failed(Error error) => error;
}