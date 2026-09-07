using Microsoft.Win32;

namespace Dawn.Common.Windows.Extensions;

public static class RegistryKeyEx
{
    extension(RegistryKey key)
    {
        public T? GetValue<T>(string? keyName) => key.GetValue(keyName) is T value 
            ? value 
            : default;
    }
}
