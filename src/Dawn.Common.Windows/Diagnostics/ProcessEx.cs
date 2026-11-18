namespace Dawn.Common.Windows.Diagnostics;

public static class ProcessEx
{
    extension(Process)
    {
        public static Result<Process> GetShellProcess()
        {
            _ = GetWindowThreadProcessId(GetShellWindow(), out var shellPid);

            try
            {
                return Process.GetProcessById((int)shellPid);
            }
            catch (Exception e)
            {
                return e;
            }
        }
    }
}