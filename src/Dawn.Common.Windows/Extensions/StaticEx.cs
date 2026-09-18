using System.Security.Principal;

namespace Dawn.Common.Windows.Extensions;

[SuppressMessage("ReSharper", "InvokeAsExtensionMemberFromSameClass")]
public static class StaticEx
{
    private static readonly Lazy<bool> isAdmin = new(() => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator));
    
    extension(Static)
    {
        [DoesNotReturn]
        public static void RestartAsAdmin(params string[] arguments)
        {
            StartAsAdmin(Environment.ProcessPath!, arguments);
            
            Environment.Exit(0);
        }

        public static void StartAsAdmin(string file, params string[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = file,
                Verb = "runas",
                UseShellExecute = true,
            };
            foreach (var argument in arguments) 
                startInfo.ArgumentList.Add(argument);

            try
            {
                Process.Start(startInfo);
            }
            catch
            {
                // ignored
            }
        }
    
        public static bool IsAdmin() => isAdmin.Value;
    }
}