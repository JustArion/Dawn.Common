using Microsoft.Win32;
using Log = Serilog.Log;

namespace Dawn.Common.Windows.Extensions;

public static class RegistryEx
{
    extension(RegistryKey registry)
    {
        public RegistryKey CreateOrOpenSubKey(string subkey, bool writable = false)
        {
            Log.Verbose("Opening {SubKey} in '{Path}' [Writable: {Writable}]", subkey, registry.Name, writable);
            return registry.OpenSubKey(subkey, writable) ?? registry.CreateSubKey(subkey, writable);
        }
    }
}
