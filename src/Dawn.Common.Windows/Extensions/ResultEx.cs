namespace Dawn.Common.Windows.Extensions;

public static class ResultEx
{
    extension(Result)
    {
        public static Error FromLastError() => GetLastError().GetException()!;
    }
}