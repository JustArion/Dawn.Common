using System.IO.Pipes;

namespace Dawn.Common.Windows.Extensions;

public static class NamedPipeStreamEx
{
    extension(NamedPipeClientStream client)
    {
        public Result<int> ServerProcessId =>
            GetNamedPipeServerProcessId(client.SafePipeHandle.DangerousGetHandle(), out var pid)
                ? (int)pid
                : Result<int>.Failed with
                {
                    Exception = GetLastError().GetException()
                };
    }

    extension(NamedPipeServerStream server)
    {
        public Result<int> ClientProcessId =>
            GetNamedPipeClientProcessId(server.SafePipeHandle.DangerousGetHandle(), out var pid)
                ? (int)pid
                : Result<int>.Failed with
                {
                    Exception = GetLastError().GetException()
                };
    }
}
