using System.Reactive;
using System.Reactive.Subjects;

namespace Dawn.Common.Models;

public class ProcessBinding : IDisposable
{
    private readonly Process? _boundProcess;
    private CancellationTokenSource? _exitWaitCts;
    
    /// <summary>
    /// Subscribing to the exit via this removes the responsibility of exiting to the subscriber 
    /// </summary>
    /// <remarks>The subject is the exit code of the bound process</remarks>
    public Subject<int> BoundProcessExited { get; } = new();

    public ProcessBinding(int pid)
    {
        try
        {
            _boundProcess = Process.GetProcessById(pid);

            SubscribeOrWaitForExit(_boundProcess);

            Log.Information("Bound to process ({Pid})", pid);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to bind to process ({Pid})", pid);
        }

    }

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
        Log.Information("Bound process has exited (Exit Code: {ExitCode})", exitCode);
        
        if (BoundProcessExited.HasObservers)
            BoundProcessExited.OnNext(exitCode);
        else 
            Environment.Exit(exitCode);
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
        _exitWaitCts?.Cancel();
        _boundProcess?.Dispose();
    }
}
