using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace Dawn.Common.Windows.Extensions;

[SuppressMessage("ReSharper", "InvokeAsExtensionMemberFromSameClass")]
public static class X509Certificate2Ex
{
    extension(X509Certificate2)
    {
        public static bool IsSignedFile(FileInfo file)
        {
            try
            {
                return X509Certificate2.GetCertContentType(file.FullName) != X509ContentType.Unknown;
            }
            catch (CryptographicException) // Cannot find the requested object.
            {
                return false;
            }
        }
        
        [SuppressMessage("ReSharper", "SwitchStatementHandlesSomeKnownEnumValuesWithDefault")]
        public static X509Certificate2? FromFile(FileInfo file)
        {
            if (!IsSignedFile(file))
                return null;

            var contentType = X509Certificate2.GetCertContentType(file.FullName);
            switch (contentType)
            {
                case X509ContentType.Authenticode:
                    #pragma warning disable SYSLIB0057
                    // https://github.com/dotnet/docs/issues/41662#issuecomment-2214051800
                    // https://github.com/dotnet/docs/issues/41662#issuecomment-2214730902
                    return new X509Certificate2(file.FullName);
                    #pragma warning restore SYSLIB0057
                case X509ContentType.Pkcs7:
                    return FromFilePkcs7(file);
                default:
                    return X509CertificateLoader.LoadCertificateFromFile(file.FullName);
            }
        }
        
        // https://learn.microsoft.com/en-us/dotnet/fundamentals/syslib-diagnostics/syslib0057#workaround
        public static X509Certificate2? FromFilePkcs7(FileInfo file, bool checkSignature = false)
        {
            try
            {
                var contentType = X509Certificate2.GetCertContentType(file.FullName);
                if (contentType != X509ContentType.Pkcs7)
                    return null;

                var content = File.ReadAllBytes(file.FullName);
                var contentInfo = new ContentInfo(content);
                
                var signedCms = new SignedCms(contentInfo);
                signedCms.Decode(content);
                signedCms.CheckSignature(checkSignature);
                
                return signedCms.SignerInfos[0].Certificate;
            }
            catch (CryptographicException)
            {
                return null;
            }
        }
    }
}