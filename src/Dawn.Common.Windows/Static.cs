using System.Security.Principal;
using System.Windows.Forms;

namespace Dawn.Common.Windows;

public static class Static
{
    [DoesNotReturn]
    public static void RestartAsAdmin(params string[] arguments)
    {
        StartAsAdmin(Environment.ProcessPath!, arguments);
            
        Environment.Exit(0);

        var x = Application.ProductName;
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
    
    private static readonly Lazy<bool> isAdmin = new(() => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator));

    public static bool IsAdmin() => isAdmin.Value;
}

