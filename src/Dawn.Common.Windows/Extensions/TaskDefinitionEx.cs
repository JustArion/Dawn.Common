using Microsoft.Win32.TaskScheduler;

namespace Dawn.Common.Windows.Extensions;

public static class TaskDefinitionEx
{
    extension(TaskDefinition td)
    {
        public void RunAsAdmin() => td.Principal.RunLevel = TaskRunLevel.Highest;
    }
}
