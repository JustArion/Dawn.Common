namespace Dawn.Common.Extensions;

public static class IntPtrEx
{
    extension(nint)
    {
        public static nint Add(nint ptr, long offset) => (nint)(ptr + offset);
        public static nint Add(nint ptr, ulong offset) => nint.Add(ptr, (long)offset);
    }
    
    extension(nuint)
    {
        public static nuint Add(nuint ptr, ulong offset) => (nuint)(ptr + offset);
        public static nuint Add(nuint ptr, long offset) => nuint.Add(ptr, (ulong)offset);
    }
}