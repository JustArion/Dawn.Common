namespace Dawn.Common.Windows.Extensions;

public static class ResultEx
{
    extension(Result)
    {
        public static Result FromLastError() => Result.Failed(GetLastError().GetException()!);
    }
}