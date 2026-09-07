using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Dawn.Common.Windows.Diagnostics.Models;
using static Vanara.PInvoke.VersionDll;

namespace Dawn.Common.Windows.Diagnostics;

[SupportedOSPlatform("windows")]
public partial class ExFileVersionInfo
{
    private unsafe ExFileVersionInfo(string fileName)
    {
        FileName = fileName;

        const FILE_VER_GET FLAGS = FILE_VER_GET.FILE_VER_GET_LOCALISED | FILE_VER_GET.FILE_VER_GET_NEUTRAL;
        if (GetFileVersionInfoSizeEx(FLAGS, fileName, out _) is not (> 0 and var size)) 
            return;
        
        var buffer = (nint)NativeMemory.AllocZeroed(size);
        try
        {
            if (!GetFileVersionInfoEx(FLAGS, fileName, 0, size, buffer))
                return;
            
            var lcp = GetLanguageAndCodePage(buffer);
            if (GetVersionInfoForCodePage(buffer, lcp))
                return;

            if (lcp != LCPID.EN_US_UTF16 && GetVersionInfoForCodePage(buffer, LCPID.EN_US_UTF16))
                return;
            
            if (lcp != LCPID.EN_US_ANSI && GetVersionInfoForCodePage(buffer, LCPID.EN_US_ANSI))
                return;

            GetVersionInfoForCodePage(buffer, LCPID.EN_US_UNKNOWN);
        }
        finally
        {
            NativeMemory.Free((void*)buffer);
        }
    }

    private unsafe bool GetVersionInfoForCodePage(nint buffer, LCPID lcp)
    {
        // I don't think we need 256 here since the longest string we create is \\StringFileInfo\\{lcp:X8}\\OriginalFilename (44)
        Span<char> qBuffer = stackalloc char[64];

        CompanyName     ??= GetValue(buffer, CreateQuery(qBuffer, nameof(CompanyName)));
        FileDescription ??= GetValue(buffer, CreateQuery(qBuffer, nameof(FileDescription)));
        FileVersion     ??= GetValue(buffer, CreateQuery(qBuffer, nameof(FileVersion)));
        
        InternalName     ??= GetValue(buffer, CreateQuery(qBuffer, nameof(InternalName)));
        LegalCopyright   ??= GetValue(buffer, CreateQuery(qBuffer, nameof(LegalCopyright)));
        OriginalFilename ??= GetValue(buffer, CreateQuery(qBuffer, nameof(OriginalFilename)));
        ProductName      ??= GetValue(buffer, CreateQuery(qBuffer, nameof(ProductName)));
        ProductVersion   ??= GetValue(buffer, CreateQuery(qBuffer, nameof(ProductVersion)));
        Comments         ??= GetValue(buffer, CreateQuery(qBuffer, nameof(Comments)));
        LegalTrademarks  ??= GetValue(buffer, CreateQuery(qBuffer, nameof(LegalTrademarks)));
        PrivateBuild     ??= GetValue(buffer, CreateQuery(qBuffer, nameof(PrivateBuild)));
        SpecialBuild     ??= GetValue(buffer, CreateQuery(qBuffer, nameof(SpecialBuild)));

        Language ??= GetFileVersionLanguage(buffer);

        var ffi = GetFixedFileInfo(buffer);
        FileMajorPart      = Macros.HIWORD(ffi.dwFileVersionMS);
        FileMinorPart      = Macros.LOWORD(ffi.dwFileVersionMS);
        FileBuildPart      = Macros.HIWORD(ffi.dwFileVersionLS);
        FilePrivatePart    = Macros.LOWORD(ffi.dwFileVersionLS);
        ProductMajorPart   = Macros.HIWORD(ffi.dwProductVersionMS);
        ProductMinorPart   = Macros.LOWORD(ffi.dwProductVersionMS);
        ProductBuildPart   = Macros.HIWORD(ffi.dwProductVersionLS);
        ProductPrivatePart = Macros.LOWORD(ffi.dwProductVersionLS);

        IsDebug        = ffi.dwFileFlags.HasFlag(VS_FF.VS_FF_DEBUG);
        IsPatched      = ffi.dwFileFlags.HasFlag(VS_FF.VS_FF_PATCHED);
        IsPrivateBuild = ffi.dwFileFlags.HasFlag(VS_FF.VS_FF_PRIVATEBUILD);
        IsPreRelease   = ffi.dwFileFlags.HasFlag(VS_FF.VS_FF_PRERELEASE);
        IsSpecialBuild = ffi.dwFileFlags.HasFlag(VS_FF.VS_FF_SPECIALBUILD);
        
        return FileVersion != string.Empty;
        string CreateQuery(Span<char> buf, string name)
        {
            buf.Clear();
            return string.Create(null, buf, $@"\\StringFileInfo\\{lcp.Value:X8}\\{name}");
        }
    }

    private static unsafe VS_FIXEDFILEINFO GetFixedFileInfo(nint handle) =>
        VerQueryValue(handle, @"\", out var qBuffer, out _) 
            ? Unsafe.Read<VS_FIXEDFILEINFO>(qBuffer.ToPointer()) 
            : default;

    private static string GetFileVersionLanguage(nint handle)
    {
        var langId = GetLanguageAndCodePage(handle).LanguageId.Value;

        var buffer = new StringBuilder(MAX_PATH);
        _ = VerLanguageName(langId, buffer, (uint)buffer.Capacity);
        return buffer.ToString();
    }

    private static string GetValue(nint buffer, string path) => 
        VerQueryValue(buffer, path, out var value, out _) 
            ? Marshal.PtrToStringUni(value)! 
            : string.Empty;

    private static unsafe LCPID GetLanguageAndCodePage(nint buffer)
    {
        if (!VerQueryValue(buffer, @"\VarFileInfo\Translation", out var qBuffer, out _))
            return LCPID.EN_US_ANSI;
        
        var span = new ReadOnlySpan<ushort>(qBuffer.ToPointer(), 2);
        // return Macros.MAKELONG(BinaryPrimitives.ReadUInt16LittleEndian(span[sizeof(ushort)..]), BinaryPrimitives.ReadUInt16LittleEndian(span));
        return new(new(span[0]), (CodePage)span[1]);
    }
}
