using System.Reactive;
using System.Reactive.Subjects;

namespace Dawn.Common.Models;

public class ProcessBinding : IDisposable
{
    private readonly Action<int> _onExit;
    private readonly Process? _boundProcess;
    private CancellationTokenSource? _exitWaitCts;
    
    public static ProcessBinding BindTo(int processId, Action<int> onExit) => new(processId, onExit);
    public static ProcessBinding BindTo(int processId) => new(processId);
    public ProcessBinding(int pid, Action<int> onExit)
    {
        _onExit = onExit;
        try
        {
            _boundProcess = Process.GetProcessById(pid);

            SubscribeOrWaitForExit(_boundProcess);

            // Log.Debug("Bound to process ({Pid})", pid);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to bind to process ({Pid})", pid);
        }
    }

    public ProcessBinding(int pid) : this(pid, Environment.Exit) {}

    private void SubscribeOrWaitForExit(Process proc)
    {
        try
        {
            proc.EnableRaisingEvents = true;
            proc.Exited += OnProcessExit;
        }
        catch (Exception e)
        {
            Log.Verbose(e, "Unable to get notified when '{ProceName}' ({Pid}) exits. Spawning wait task instead", proc.ProcessName, proc.Id);

            _exitWaitCts = new();
            Task.Factory.StartNew(()=> WaitForProcessExitAsync(_exitWaitCts.Token), TaskCreationOptions.LongRunning);
        }
    }

    private async Task WaitForProcessExitAsync(CancellationToken token = default)
    {
        await _boundProcess!.WaitForExitAsync(token);
        OnProcessExit(this, EventArgs.Empty);
    }

    protected virtual void OnProcessExit(object? sender, EventArgs e)
    {
        var exitCode = _boundProcess!.ExitCode;
        Log.Verbose("Bound process '{ProcessName}' has exited (Exit Code: {ExitCode})", _boundProcess?.ProcessName, exitCode);

        _onExit(exitCode);
    }

    private bool _disposed;
    public virtual void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        GC.SuppressFinalize(this);
        _exitWaitCts?.Cancel();
        _boundProcess?.Dispose();
    }
}
