using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;

namespace Dawn.Common.Extensions;

[PublicAPI]
public static unsafe class SafeMemoryMappedViewHandleEx
{
    extension(SafeMemoryMappedViewHandle handle)
    {
        public T Read<T>() where T : struct => handle.Read<T>(0);

        public Span<T> ReadSpan<T>(ulong offset, int count) => new(IntPtr.Add(handle.DangerousGetHandle(), (int)offset).ToPointer(), count);
        
        public string ReadString(ulong offset) => new((char*)nint.Add(handle.DangerousGetHandle(), (int)offset).ToPointer());
    }
}