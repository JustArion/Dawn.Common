using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace Dawn.Common;

// Running specific operations as a lower-privledge 
public static class Impersonation
{
    // public static void ExecuteAsUser(Action act)
    // {
        // var explorer = GetExplorer(); // We would use a verified explorer.exe's user priv to run the action
        
        // WindowsIdentity.RunImpersonated(, )
    // }
}