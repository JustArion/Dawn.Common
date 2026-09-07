using Dawn.Common.Windows.Diagnostics;

namespace Dawn.Common.Windows.Extensions;

public static class FileVersionInfoEx
{
    extension(FileVersionInfo)
    {
        public static ExFileVersionInfo GetVersionInfoEx(string filePath) => ExFileVersionInfo.GetVersionInfo(filePath);
        public static ExFileVersionInfo GetVersionInfoEx(FileInfo file) => ExFileVersionInfo.GetVersionInfo(file);
    }

    extension(FileInfo file)
    {
        public ExFileVersionInfo GetVersionInfo() => FileVersionInfo.GetVersionInfoEx(file);
    }
}
