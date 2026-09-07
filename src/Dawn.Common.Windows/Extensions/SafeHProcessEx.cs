namespace Dawn.Common.Windows.Extensions;

public static class SafeHProcessEx
{
    extension(SafeHPROCESS proc)
    {
        public NtQueryResult<T> QueryInformation<T>(PROCESSINFOCLASS processInfo) where T : struct => NtQueryInformationProcess<T>(proc, processInfo);

        public PROCESS_BASIC_INFORMATION ProcessBasicInformation =>
            proc.QueryInformation<PROCESS_BASIC_INFORMATION>(PROCESSINFOCLASS.ProcessBasicInformation)
                .GetValueOrDefault();
    }
}