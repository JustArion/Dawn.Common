namespace Dawn.Common.Windows.Extensions;

public static class ResultEx
{
    extension(Result)
    {
        public static Result FromLastError() => Error.Failed(GetLastError().GetException()!);
    }
}