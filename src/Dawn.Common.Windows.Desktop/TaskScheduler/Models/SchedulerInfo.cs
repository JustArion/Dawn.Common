using Microsoft.Win32.TaskScheduler;

namespace Dawn.Common.Windows.TaskScheduler.Models;

public record SchedulerInfo(string Name, TaskRunLevel RunLevel, string? ExecutablePath, string? arguments, DateTime RegistrationDate, bool IsEnabled);

