namespace Dawn.Common.Windows.Extensions;

public static class ConsoleEx
{
    private static bool? _isConsoleAttached;
    
    extension(Console)
    {
        public static bool IsConsoleAttached => _isConsoleAttached ??= AttachConsole(ATTACH_PARENT_PROCESS);
        public static bool Attach() => (_isConsoleAttached = AttachConsole(ATTACH_PARENT_PROCESS)).Value;
    }
}
