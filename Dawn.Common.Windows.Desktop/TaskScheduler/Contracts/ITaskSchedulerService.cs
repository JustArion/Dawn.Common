using Dawn.Common.Windows.TaskScheduler.Models;
using Microsoft.Win32.TaskScheduler;
using Task = Microsoft.Win32.TaskScheduler.Task;

namespace Dawn.Common.Windows.TaskScheduler.Contracts;

public interface ITaskSchedulerService
{
    public IEnumerable<SchedulerInfo> GetAllTasks(); // New method
    public Task? GetTask(string key);
    public bool IsEnabled(string key);

    public bool RunIfPresent(string key);
    
    public bool ContainsTask(string key);

    public void Enable(Task task);
    public void Disable(Task task);
    public void ToggleEnabled(Task task);

    public void Run(string key);

    public Task Add(string key, FileInfo file, ICollection<string> args, Action<TaskDefinition>? builder = null);

    public void RunOnStartup(string key, FileInfo file, params string[] args);

    public bool Remove(string key);
}

