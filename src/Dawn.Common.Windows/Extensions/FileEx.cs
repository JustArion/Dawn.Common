using System.Runtime.InteropServices;
using Dawn.Common.Extensions;
using Shortcut = ShellLink.Shortcut;

namespace Dawn.Common.Windows.Extensions;

public static class FileEx
{
    extension(FileInfo file)
    {       
        public FileInfo ResolveShortcuts()
        {
            if (file.Extension != ".lnk")
                return file;
            
            var shortcut = Shortcut.ReadFromFile(file.FullName);

            return new(shortcut.LinkTargetIDList.Path);
        }
        
        public string FriendlyName
        {
            get
            {
                var name = Common.Static.SuppressExceptions(() =>
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var info = FileVersionInfo.GetVersionInfoEx(file);

                        return string.IsNullOrWhiteSpace(info.ProductName) 
                            ? info.FileDescription 
                            : info.ProductName;
                    }
                    else
                    {
                        var info = FileVersionInfo.GetVersionInfo(file.FullName);

                        return string.IsNullOrWhiteSpace(info.ProductName) 
                            ? info.FileDescription 
                            : info.ProductName;
                    }
                });

                name = string.IsNullOrWhiteSpace(name) 
                    ? null 
                    : name;

                name ??= file.Name.Replace(".exe", string.Empty);
                return name;
            }
        }

        public bool Equals(FileInfo? other, FileInfoComparer? comparer = null) => (comparer ?? FileInfoComparer.Default).Equals(file, other);
        public bool Equals(string? other, FileInfoComparer? comparer = null) => (comparer ?? FileInfoComparer.Default).Equals(other, file);
    }
}

