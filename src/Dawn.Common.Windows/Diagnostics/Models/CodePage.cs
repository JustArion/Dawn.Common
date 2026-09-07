namespace Dawn.Common.Windows.Diagnostics.Models;

// https://learn.microsoft.com/en-us/windows/win32/intl/code-page-identifiers
internal enum CodePage : ushort
{
    Unknown = 0x0000,
    // Unicode UTF-16, little endian byte order (BMP of ISO 10646); available only to managed applications
    UTF16 = 0x04B0,
    // ANSI Latin 1; Western European (Windows)
    ANSI = 0x04E4
}