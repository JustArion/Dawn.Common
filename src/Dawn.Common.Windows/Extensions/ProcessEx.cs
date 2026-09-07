using System.Collections.Concurrent;
using System.Text;

namespace Dawn.Common.Windows.Extensions;

public static class ProcessEx
{
    internal readonly record struct WeakId(int PID, DateTime StartTime)
    {
        public static implicit operator WeakId(Process proc) => new(proc.Id, Common.Static.SuppressExceptions(() => proc.StartTime));

        public static bool operator ==(WeakId? a, WeakId? b) => a?.PID == b?.PID && a?.StartTime == b?.StartTime;
        public static bool operator !=(WeakId? a, WeakId? b) => !(a == b);
    }
    
    internal static readonly ConcurrentDictionary<WeakId, int> _parentProcessIdCache = [];
    internal static readonly ConcurrentDictionary<WeakId, FileInfo> _processFilePathCache = [];
    internal const int EVICTION_LIMIT = 100;
    internal static int GetParentProcessId(Process proc)
    {
        var safeProc = new SafeHPROCESS(proc.Handle, false);
        
        return (int)safeProc.ProcessBasicInformation.InheritedFromUniqueProcessId;
    }

    internal static Result<string> GetProcessFilePath(Process proc)
    {
        var sb = new StringBuilder(MAX_PATH);
        var length = (uint)sb.Capacity + 1;
        return QueryFullProcessImageName(proc.Handle, PROCESS_NAME.PROCESS_NAME_WIN32, sb, ref length) 
            ? sb.ToString() 
            : Result.Failed(GetLastError().GetException()!);
    }
    
    private static bool IsSystemProcess(Process proc) => proc.Id <= SYSTEM_ID || Common.Static.SuppressExceptions(()=> proc.StartTime) == default;

    private const int SYSTEM_ID = 4;
    private const int IDLE_ID = 0;
    extension(Process proc)
    {
        public Result<int> ParentId
        {
            get
            {
                try
                {
                    if (IsSystemProcess(proc))
                        return SYSTEM_ID;
                
                    if (_parentProcessIdCache.TryGetValue(proc, out var parent))
                        return parent;

                    parent = GetParentProcessId(proc);
                    // Extended system processes will error since they also don't have start times, throwing an exception
                    // So they will never get cached
                    if (!_parentProcessIdCache.TryAdd(proc, parent) || _parentProcessIdCache.Count <= EVICTION_LIMIT) 
                        return parent;

                    // The oldest entry is removed. [2] -> [1] -> [0]
                    var oldest = _parentProcessIdCache.First();
                    _parentProcessIdCache.TryRemove(oldest);

                    return parent;
                }
                catch (Exception e)
                {
                    return Result.Failed(e);
                }
            }
        }

        public Result<Process> GetParentProcess()
        {
            try
            {
                return Process.GetProcessById(proc.ParentId);
            }
            catch (Exception e)
            {
                return Result.Failed(e);
            }
        }

        public Result<FileInfo> FilePath
        {
            get
            {
                try
                {
                    if (proc.Id == 0)
                        return Result.Failed(new InvalidOperationException("System Idle does not have a file path"));

                    if (_processFilePathCache.TryGetValue(proc, out var filePath))
                        return filePath;

                    GetProcessFilePath(proc).Deconstruct(out var success, out var exception, out var value);

                    if (!success)
                        return Result.Failed(exception!);

                    var fi = new FileInfo(value!);

                    if (!_processFilePathCache.TryAdd(proc, fi) || _processFilePathCache.Count <= EVICTION_LIMIT)
                        return fi;

                    // The oldest entry is removed. [2] -> [1] -> [0]
                    var oldest = _processFilePathCache.First();
                    _processFilePathCache.TryRemove(oldest);

                    return fi;
                }
                catch (Exception e)
                {
                    return Result.Failed(e);
                }
            }
        }
    }
}