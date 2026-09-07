using System.Diagnostics.CodeAnalysis;

namespace Dawn.Common;

public readonly struct Result
{
    public required bool Success { get; init; }
    
    [MemberNotNullWhen(false, nameof(Success))]
    public Exception? Exception { get; init; }
    
    public static readonly Result Failure = new() { Success = false };

    public static Result Failed(Exception ex) => Failure with { Exception = ex };
    public static Result Failed<T>(Result<T> tres) => Failure with { Exception = tres.Exception };
}

// ---

public readonly struct Result<T>
{
    public override string ToString() => Value == null ? Exception?.ToString() ?? string.Empty : Value.ToString()!;

    [
        MemberNotNullWhen(true, nameof(Value)),
        MemberNotNullWhen(false, nameof(Exception))
    ]
    public required bool Success { get; init; }
    
    public T? Value { get; init; }
    
    public Exception? Exception { get; init; }
    
    public static implicit operator bool(Result<T> result) => result.Success;
    
    public static implicit operator T?(Result<T> result) => result.Value;
    
    public static implicit operator Result<T>(T? value) => new() { Success = true, Value = value };

    public static implicit operator Result<T>(Result res) => new() { Success = res.Success, Exception = res.Exception };

    public static implicit operator Result<T>(bool value) =>
        typeof(T) != typeof(bool) 
            ? new Result<T> { Success = value } 
            : throw new NotSupportedException("Only non-bools are supported");
    
    public void Deconstruct(out bool success, out T? value)
    {
        success = Success;
        value = Value;
    }
    
    public void Deconstruct(out bool success, out Exception? exception, out T? value)
    {
        success = Success;
        value = Value;
        exception = Exception;
    }

    public static readonly Result<T> Failed = new() { Success = false };
}