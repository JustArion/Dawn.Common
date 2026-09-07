using System.Diagnostics.CodeAnalysis;
using System.Windows.Threading;
using Dawn.Common.Windows.TaskScheduler.Contracts;
using Dawn.Common.Windows.TaskScheduler.Models;
using Microsoft.Win32.TaskScheduler;
using Action = System.Action;
using Task = Microsoft.Win32.TaskScheduler.Task;

namespace Dawn.Common.Windows.TaskScheduler;

public class TaskSchedulerService : ITaskSchedulerService, IDisposable
{
    private readonly Lazy<TaskService> _scheduler;
    private readonly Lazy<TaskFolder> Folder;

    private readonly Thread _staThread;
    private Dispatcher? _dispatcher;

    private T ExecuteOnDedicatedStaThread<T>(Func<T> func)
    {
        return _dispatcher != null 
            ? _dispatcher.Invoke(func) 
            : throw new InvalidOperationException("Dispatcher not initialized");
    }

    private void ExecuteOnDedicatedStaThread(Action action)
    {
        if (_dispatcher == null)
            throw new InvalidOperationException("Dispatcher not initialized");
        _dispatcher.Invoke(action);
    }

    [SuppressMessage("ReSharper", "InvertIf")]
    public IEnumerable<SchedulerInfo> GetAllTasks()
    {
        if (_disposed)
            ObjectDisposedException.ThrowIf(_disposed, typeof(TaskSchedulerService));

        return ExecuteOnDedicatedStaThread(() =>
        {
            var folder = Folder.Value;
            return folder.AllTasks
                .Select(task =>
                {
                    string? execActionPath = null;
                    string? execActionArguments = null;
                    if (task.Definition.Actions.FirstOrDefault(a => a.ActionType == TaskActionType.Execute) is ExecAction execAction)
                    {
                        execActionPath = execAction.Path;
                        execActionArguments = execAction.Arguments;
                    }

                    return new SchedulerInfo(
                        task.Name,
                        task.Definition.Principal.RunLevel,
                        execActionPath,
                        execActionArguments,
                        task.Definition.RegistrationInfo.Date,
                        task.Enabled
                    );
                })
                .ToList();
        });
    }

    public void ToggleEnabled(Task task)
    {
        ExecuteOnDedicatedStaThread(() =>
        {
            task.Definition.Settings.Enabled ^= true;
            task.RegisterChanges();
        });
    }
    public void Enable(Task task)
    {
        ExecuteOnDedicatedStaThread(() =>
        {
            task.Definition.Settings.Enabled = true;
            task.RegisterChanges();
        });
    }

    public void Disable(Task task)
    {
        ExecuteOnDedicatedStaThread(() =>
        {
            task.Definition.Settings.Enabled = false;
            task.RegisterChanges();
        });
    }

    [SuppressMessage("Performance", "CA1826:Do not use Enumerable methods on indexable collections")] // We need it since the result could be null
    public Task? GetTask(string key)
    {
        if (_disposed)
            ObjectDisposedException.ThrowIf(_disposed, typeof(TaskSchedulerService));

        return ExecuteOnDedicatedStaThread(() =>
        {
            Task? task;
            if (string.IsNullOrWhiteSpace(_folderName))
                task = _scheduler.Value.GetTask(key);
            else
            {
                var folder = Folder.Value;
                task = folder.Tasks.FirstOrDefault(x => x.Name == key);
            }
            return task;
        });
    }
    
    public bool IsEnabled(string key)
    {
        return ExecuteOnDedicatedStaThread(() =>
        {
            try
            {
                if (!ContainsTask(key))
                    return false;

                using var task = GetTask(key);

                return task?.Definition.Actions.FirstOrDefault(x => x.ActionType == TaskActionType.Execute) is ExecAction;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to check if task '{TaskName}' is enabled", key);
                return false;
            }
        });
    }

    public bool RunIfPresent(string key)
    {
        return ExecuteOnDedicatedStaThread(() =>
        {
            if (!IsEnabled(key))
            {
                _logger.Warning("Task '{TaskName}' is not present", key);
                return false;
            }

            Run(key);
            _logger.Verbose("Task '{TaskName}' is present, elevating...", key);
            return true;
        });
    }

    public bool ContainsTask(string key)
    {
        return ExecuteOnDedicatedStaThread(() =>
        {
            try
            {
                using var task = GetTask(key);

                return task?.Definition.Actions.FirstOrDefault() is ExecAction;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to check if task '{TaskName}' is present", key);
                return false;
            }
        });
    }

    public void Run(string key)
    {
        ExecuteOnDedicatedStaThread(() =>
        {
            try
            {
                GetTask(key)?.Run();
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to run task '{TaskName}'", key);
            }
        });
    }

    // There's a Task Scheduler bug where it fails to set the UserId to a valid one
    // eg. Dawn/Dawn is the <computer name>/<username>
    // UserId is sometimes set to Dawn instead of Dawn/Dawn in this case, task scheduler errors due to this since there's no valid UserId
    // So we fix it by changing i
    private static void FixSchedulerBugIfNecessary(TaskPrincipal tp)
    {
        if (tp.UserId == Environment.UserName)
            tp.UserId = tp.Account;
    }
    
    public Task Add(string key, FileInfo file, ICollection<string> args, Action<TaskDefinition>? builder = null)
    {
        if (_disposed)
            ObjectDisposedException.ThrowIf(_disposed, typeof(TaskSchedulerService));

        return ExecuteOnDedicatedStaThread(() =>
        {
            try
            {
                Task task;
                using var td = _scheduler.Value.NewTask();
                try
                {
                    var settings = td.Settings;
                    settings.AllowDemandStart = true;
                    settings.StopIfGoingOnBatteries = false;
                    settings.DisallowStartIfOnBatteries = false;
                    td.Actions.Add(new ExecAction(file.Name, string.Join(' ', args), file.Directory?.FullName));

                    FixSchedulerBugIfNecessary(td.Principal);
                    builder?.Invoke(td);
                }
                finally
                {
                    task = Folder.Value.RegisterTaskDefinition(key, td);
                    task.Enabled = true;
                    task.RegisterChanges();
                }

                return task;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to create a task '{TaskName}'", key);
                throw;
            }
        });
    }

    public void RunOnStartup(string key, FileInfo file, params string[] args)
    {
        throw new NotImplementedException();
    }

    public bool Remove(string key)
    {
        if (_disposed)
            ObjectDisposedException.ThrowIf(_disposed, typeof(TaskSchedulerService));
        
        return ExecuteOnDedicatedStaThread(() =>
        {
            try
            {
                if (!ContainsTask(key))
                {
                    _logger.Warning("Task '{TaskName}' is not present", key);
                    return false;
                }

                Folder.Value.DeleteTask(key, false);
                _logger.Information("Task '{TaskName}' is removed", key);
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to remove task '{TaskName}'", key);
                return false;
            }
        });
    }

    private volatile bool _disposed;
    private readonly ILogger _logger;
    private readonly string _folderName;

    
    // We had to make the entire class here thread safe since I experienced a lot of pain regarding COM's Single Threaded Apartments >:c
    // https://github.com/dahall/TaskScheduler/issues/84
    public TaskSchedulerService(ILogger logger, string folderName = "")
    {
        _logger = logger;
        _folderName = folderName;

        var mre = new ManualResetEventSlim();

        _staThread = new Thread(() =>
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            mre.Set();
            Dispatcher.Run();
        });
        _staThread.SetApartmentState(ApartmentState.STA);
        _staThread.IsBackground = true;
        _staThread.Start();
        mre.Wait(TimeSpan.FromSeconds(1));

        _scheduler = new Lazy<TaskService>(() => ExecuteOnDedicatedStaThread(() => new TaskService()));

        Folder = new Lazy<TaskFolder>(() => ExecuteOnDedicatedStaThread(() =>
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return _scheduler.Value.RootFolder;

            var folder = _scheduler.Value.GetFolder(folderName) ?? _scheduler.Value.RootFolder.CreateFolder(folderName);
            return folder;
        }));
    }

    private void ReleaseUnmanagedResources()
    {
        if (_disposed)
            ObjectDisposedException.ThrowIf(_disposed, typeof(TaskSchedulerService));
        _disposed = true;

        ExecuteOnDedicatedStaThread(() =>
        {
            if (_scheduler.IsValueCreated) 
                _scheduler.Value.Dispose();
            
            if (Folder.IsValueCreated) 
                Folder.Value.Dispose();
        });

        if (_dispatcher == null) 
            return;
        
        _dispatcher.InvokeShutdown();
        _staThread.Join();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~TaskSchedulerService() => ReleaseUnmanagedResources();
}

