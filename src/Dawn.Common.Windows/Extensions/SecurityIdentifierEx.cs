using System.Security.Principal;

namespace Dawn.Common.Windows.Extensions;

[SuppressMessage("ReSharper", "InvokeAsExtensionMemberFromSameClass")]
public static class SecurityIdentifierEx
{
    extension(SecurityIdentifier)
    {
        public static SecurityIdentifier GetSecurityIdentifier(WellKnownSidType type) => new (type, null);

        public static SecurityIdentifier AdministratorId => GetSecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid);
        public static SecurityIdentifier LocalSystemId => GetSecurityIdentifier(WellKnownSidType.LocalSystemSid);
        public static SecurityIdentifier UsersId => GetSecurityIdentifier(WellKnownSidType.BuiltinUsersSid);
        public static SecurityIdentifier AuthenticatedUser => GetSecurityIdentifier(WellKnownSidType.AuthenticatedUserSid);
        public static SecurityIdentifier? LoggedInUserId => WindowsIdentity.GetCurrent().User;
        
        

    }
}