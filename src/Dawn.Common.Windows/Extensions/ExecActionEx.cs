using Microsoft.Win32.TaskScheduler;

namespace Dawn.Common.Windows.Extensions;

public static class ExecActionEx
{
    extension(ExecAction execAction)
    {
        public FileInfo GetPathInfo() => new(execAction.Path);
    }
}
