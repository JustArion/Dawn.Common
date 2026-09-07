using System.Runtime.InteropServices;

namespace Dawn.Common.Windows.Diagnostics.Models;

[StructLayout(LayoutKind.Sequential, Size = sizeof(int))]
internal readonly struct LCPID(uint value) : IEquatable<LCPID>
{
    public LCPID(LANGID langId, CodePage codePage) : this((uint)codePage | (uint)langId << 16) {}

    public readonly uint Value = value;

    public CodePage CodePage => (CodePage)Value;
    public LANGID LanguageId => (LANGID)(Value >> 16);

    // Vanara's is a bit dated, we can find the latest under "Published Version" along with the spec here:
    // https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-lcid/70feba9f-294e-491e-b6eb-56532684c37f
    // According to the spec 0x0009 is "EN" and 0x0409 is EN-US. (For GB it's 0x0809, For AU it's 0x0C09)
    // So while the below const isn't per-say a universal sublang, it is required for combining into EN-US (Note: SUBLANG_ENGLISH_US can't be used as it's 0x01. Which doesn't seem right)
    private const LANGID.SUBLANG EN_US_SUBLANG = LANGID.SUBLANG.SUBLANG_CUSTOM_UNSPECIFIED;
    public static readonly LCPID EN_US_ANSI = new(new LANGID(LANGID.LANG.LANG_ENGLISH, EN_US_SUBLANG), CodePage.ANSI);
    public static readonly LCPID EN_US_UTF16 = new(new LANGID(LANGID.LANG.LANG_ENGLISH, EN_US_SUBLANG), CodePage.UTF16);
    public static readonly LCPID EN_US_UNKNOWN = new(new LANGID(LANGID.LANG.LANG_ENGLISH, EN_US_SUBLANG), CodePage.Unknown);

    public static bool operator ==(LCPID left, LCPID right) => left.Equals(right);
    public static bool operator !=(LCPID left, LCPID right) => !left.Equals(right);

    public bool Equals(LCPID other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is LCPID other && Equals(other);

    public override int GetHashCode() => (int)Value;
}
