using Microsoft.Win32.TaskScheduler;
using SchedulerTask = Microsoft.Win32.TaskScheduler.Task;

namespace Dawn.Common.Windows.Desktop.TaskScheduler.Models;

public record SchedulerInfo(
    string Name,
    TaskRunLevel RunLevel,
    FileInfo? ExecutablePath,
    string? Arguments,
    DateTime RegistrationDate,
    bool IsEnabled)
{

    [SuppressMessage("ReSharper", "InvertIf")]
    public static SchedulerInfo? FromTask(SchedulerTask? task)
    {
        if (task is null)
            return null;
        
        FileInfo? execActionPath = null;
        string? execActionArguments = null;
        if (task.Definition.Actions.FirstOrDefault(a => a.ActionType == TaskActionType.Execute) is ExecAction execAction)
        {
            execActionPath = new FileInfo(Path.Combine(execAction.WorkingDirectory, execAction.Path));
            execActionArguments = execAction.Arguments;
        }

        return new SchedulerInfo(
            task.Name,
            task.Definition.Principal.RunLevel,
            execActionPath,
            execActionArguments,
            task.Definition.RegistrationInfo.Date,
            task.Enabled);
    }


    public static implicit operator SchedulerInfo(SchedulerTask task) => FromTask(task)!;
}