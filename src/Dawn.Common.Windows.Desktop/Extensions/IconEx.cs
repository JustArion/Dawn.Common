using static Vanara.PInvoke.Shell32;

namespace Dawn.Common.Windows.Desktop.Extensions;

public static class IconEx
{
    extension(Icon)
    {
        public static Icon ExtractAssociatedIcon(string filePath, SHIL imageType = SHIL.SHIL_JUMBO)
        {
            var info = default(SHFILEINFO);
            SHGetFileInfo(filePath, FileAttributes.None, ref info, SHFILEINFO.Size,
                SHGFI.SHGFI_SYSICONINDEX);

            var imageList = SHGetImageList(imageType);

            using var hIcon = imageList.GetIcon(info.iIcon, ComCtl32.IMAGELISTDRAWFLAGS.ILD_TRANSPARENT);

            return hIcon.ToIcon();
        }
    }

    extension(User32.SafeHICON icon)
    {
        public Icon ToIcon() => Icon.FromHandle(icon.ReleaseOwnership());
    }
}
